"""Evaluation: retrieval metrics (offline-capable) and generation metrics (Azure, behind a flag).

    uv run python evals/run_eval.py                         # offline BM25 retrieval eval (CI)
    uv run python evals/run_eval.py --retriever azure       # Azure AI Search hybrid + semantic
    uv run python evals/run_eval.py --retriever azure --generation   # + answer quality (costs tokens)

Exit code 1 when a metric is below its threshold in thresholds.yaml.
"""

from __future__ import annotations

import argparse
import asyncio
import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import yaml

from ai_service.container import build_container, build_pipeline
from ai_service.llm.gateway import LLMGateway, TextDelta
from ai_service.llm.prompts import load_prompt
from ai_service.llm.schemas import GroundednessGrade
from ai_service.rag.glossary import expand_arabic
from ai_service.rag.index.memory import InMemoryIndex
from ai_service.rag.ingestion.normalize import detect_language
from ai_service.rag.ingestion.repo_docs import repo_documents
from ai_service.rag.retrieval.context import assemble_context, extract_citations
from ai_service.rag.retrieval.search import Retriever
from ai_service.rag.retrieval.security import SecurityScope
from ai_service.rag.types import RetrievedChunk, SearchQuery
from ai_service.settings import ModelRole, Settings
from ai_service.tokens import TokenCounter

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[1]


@dataclass(frozen=True)
class Item:
    id: str
    language: str
    question: str
    answerable: bool
    expected: list[tuple[str, str]]


def load_dataset(path: Path) -> list[Item]:
    items: list[Item] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        row = json.loads(line)
        items.append(
            Item(
                id=row["id"],
                language=row["language"],
                question=row["question"],
                answerable=row["answerable"],
                expected=[(s["document_id"], s["heading"]) for s in row["expected_sources"]],
            )
        )
    return items


def relevant(chunk: RetrievedChunk, expected: list[tuple[str, str]]) -> bool:
    sections = set(chunk.headings) | set(chunk.heading_path.split(" › "))
    return any(chunk.document_id == doc and heading in sections for doc, heading in expected)


def retrieval_metrics(
    results: dict[str, list[RetrievedChunk]], items: list[Item], k: int
) -> dict[str, float]:
    answerable = [i for i in items if i.answerable]
    hits = {1: 0, 3: 0, k: 0}
    rr = 0.0
    for item in answerable:
        ranked = results[item.id][:k]
        rank = next((n for n, c in enumerate(ranked, 1) if relevant(c, item.expected)), None)
        if rank is not None:
            rr += 1 / rank
            for cutoff in hits:
                if rank <= cutoff:
                    hits[cutoff] += 1
    n = max(1, len(answerable))
    return {
        "questions": float(len(answerable)),
        "recall_at_1": hits[1] / n,
        "recall_at_3": hits[3] / n,
        f"recall_at_{k}": hits[k] / n,
        "mrr": rr / n,
    }


async def run_retrieval(
    retriever: Retriever, items: list[Item], scope: SecurityScope, k: int
) -> dict[str, list[RetrievedChunk]]:
    out: dict[str, list[RetrievedChunk]] = {}
    for item in items:
        extra = expand_arabic(item.question) if item.language == "ar" else ""
        out[item.id] = await retriever.retrieve(
            SearchQuery(text=item.question, language=item.language, extra_keywords=extra, top=k), scope
        )
    return out


async def build_memory_retriever(settings: Settings) -> InMemoryIndex:
    index = InMemoryIndex()
    pipeline = build_pipeline(settings, TokenCounter(settings.tokenizer_mode), writer=index, gateway=None)
    for doc in repo_documents(REPO_ROOT, settings):
        await pipeline.run(doc)
    return index


REFUSAL_MARKERS = (
    "couldn't find",
    "could not find",
    "don't know",
    "not in the",
    "لم أجد",
    "لا أعرف",
    "ليس في",
)


async def answer_once(
    gateway: LLMGateway, settings: Settings, counter: TokenCounter, item: Item, chunks: list[RetrievedChunk]
) -> tuple[str, str, bool]:
    context = assemble_context(chunks, counter, settings.context_budget_tokens)
    items: list[dict[str, object]] = []
    if not context.empty:
        items.append({"role": "developer", "content": f"<sources>\n{context.text}\n</sources>"})
    items.append({"role": "user", "content": f"locale: {item.language}\n\n{item.question}"})
    stream = await gateway.open_text_stream(
        ModelRole.CHAT,
        instructions=load_prompt("answer", settings.answer_prompt_version),
        input_items=items,
        max_output_tokens=settings.chat_max_output_tokens,
        temperature=settings.chat_temperature,
    )
    parts = [e.text async for e in stream if isinstance(e, TextDelta)]
    answer = "".join(parts)
    cited = bool(extract_citations(answer, context.citations))
    return answer, context.text, cited


async def run_generation(
    gateway: LLMGateway, settings: Settings, items: list[Item], retrieved: dict[str, list[RetrievedChunk]]
) -> dict[str, float]:
    counter = TokenCounter(settings.tokenizer_mode)
    grounded = cited_n = refused_ok = lang_ok = 0
    answerable = [i for i in items if i.answerable]
    unanswerable = [i for i in items if not i.answerable]
    for item in items:
        answer, sources, cited = await answer_once(gateway, settings, counter, item, retrieved[item.id][:6])
        grade, _ = await gateway.parse(
            ModelRole.REASONING,
            GroundednessGrade,
            instructions=load_prompt("groundedness", "v1"),
            input_items=[
                {
                    "role": "user",
                    "content": f"<question>{item.question}</question>\n"
                    f"<sources>{sources}</sources>\n<answer>{answer}</answer>",
                }
            ],
            max_output_tokens=2000,
            temperature=None,
        )
        refused = grade.refused or any(m in answer for m in REFUSAL_MARKERS)
        lang_ok += detect_language(answer) == item.language
        if item.answerable:
            grounded += grade.grounded and not refused
            cited_n += cited
        else:
            refused_ok += refused
    return {
        "groundedness": grounded / max(1, len(answerable)),
        "citation_rate": cited_n / max(1, len(answerable)),
        "refusal_accuracy": refused_ok / max(1, len(unanswerable)),
        "language_match": lang_ok / max(1, len(items)),
    }


def check(metrics: dict[str, float], minimums: dict[str, float], label: str) -> list[str]:
    return [
        f"{label}: {name} {metrics.get(name, 0.0):.3f} < {minimum:.2f}"
        for name, minimum in minimums.items()
        if metrics.get(name, 0.0) < minimum
    ]


async def main_async(args: argparse.Namespace) -> int:
    settings = Settings()
    thresholds: dict[str, Any] = yaml.safe_load((HERE / "thresholds.yaml").read_text(encoding="utf-8"))
    scope = SecurityScope(tenant_id=settings.default_tenant_id, groups=(settings.public_acl_group,))
    container = None
    gateway: LLMGateway | None = None
    if args.retriever == "memory":
        retriever: Retriever = await build_memory_retriever(settings)
        if args.generation:
            container = await build_container(settings.model_copy(update={"search_backend": "memory"}))
            gateway = container.gateway
    else:
        container = await build_container(settings.model_copy(update={"search_backend": "azure"}))
        retriever, gateway = container.retriever, container.gateway

    report: dict[str, Any] = {
        "retriever": args.retriever,
        "dataset_version": thresholds.get("dataset_version"),
        "versions": {
            "answer_prompt": settings.answer_prompt_version,
            "chunking": [settings.chunk_min_tokens, settings.chunk_max_tokens, settings.chunk_overlap_ratio],
            "tokenizer": "tiktoken" if TokenCounter(settings.tokenizer_mode).exact else "estimate",
            "embed_deployment": settings.deployments.embed or None,
        },
        "languages": {},
    }
    failures: list[str] = []
    try:
        for language in args.languages:
            items = load_dataset(HERE / "datasets" / f"qala_rules_{language}.jsonl")
            retrieved = await run_retrieval(retriever, items, scope, args.k)
            metrics = retrieval_metrics(retrieved, items, args.k)
            entry: dict[str, Any] = {"retrieval": metrics}
            failures += check(
                metrics, thresholds["retrieval"][args.retriever][language], f"{language} retrieval"
            )
            if args.verbose:
                for item in items:
                    if item.answerable:
                        top = retrieved[item.id][:3]
                        ok = any(relevant(c, item.expected) for c in retrieved[item.id][: args.k])
                        print(
                            f"  {'ok ' if ok else 'MISS'} {item.id} {item.question[:60]!r} -> "
                            f"{[c.heading_path.split(' › ')[-1][:30] for c in top]}"
                        )
            if args.generation:
                if gateway is None:
                    print("--generation needs AZURE_OPENAI_ENDPOINT and deployments", file=sys.stderr)
                    return 2
                gen = await run_generation(gateway, settings, items, retrieved)
                entry["generation"] = gen
                failures += check(gen, thresholds["generation"], f"{language} generation")
            report["languages"][language] = entry
    finally:
        if container is not None:
            await container.aclose()

    print(json.dumps(report, indent=2, ensure_ascii=False))
    if args.json:
        args.json.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    if failures:
        print("\nFAILED thresholds:\n  " + "\n  ".join(failures), file=sys.stderr)
        return 1
    print("\nAll thresholds met.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--retriever", choices=["memory", "azure"], default="memory")
    parser.add_argument("--generation", action="store_true", help="also grade answers (needs Azure OpenAI)")
    parser.add_argument("--languages", nargs="+", default=["en", "ar"])
    parser.add_argument("--k", type=int, default=5)
    parser.add_argument("--json", type=Path, help="write the report to this file")
    parser.add_argument("-v", "--verbose", action="store_true")
    return asyncio.run(main_async(parser.parse_args()))


if __name__ == "__main__":
    raise SystemExit(main())
