"""Ingest the repo's rules and lessons into the knowledge index through the normal pipeline.

    uv run python scripts/ingest_repo_docs.py --dry-run            # print chunks, no Azure
    uv run python scripts/ingest_repo_docs.py --create-index        # Azure: index schema, embed, upsert

Non-dry runs read the same settings as the service (AZURE_OPENAI_ENDPOINT, DEPLOYMENTS__EMBED,
SEARCH_ENDPOINT, SEARCH_INDEX_NAME, ...) and authenticate with Entra ID (or local keys).
"""

from __future__ import annotations

import argparse
import asyncio
import sys
from pathlib import Path

from ai_service.container import build_pipeline
from ai_service.llm.client import build_client
from ai_service.llm.gateway import LLMGateway
from ai_service.rag.ingestion.repo_docs import repo_documents
from ai_service.settings import Settings
from ai_service.tokens import TokenCounter

DEFAULT_ROOT = Path(__file__).resolve().parents[2]


def dry_run(settings: Settings, root: Path, show_text: bool) -> int:
    counter = TokenCounter(settings.tokenizer_mode)
    pipeline = build_pipeline(settings, counter, writer=None, gateway=None)
    docs = repo_documents(root, settings)
    if not docs:
        print(f"No documents found under {root / 'docs'}", file=sys.stderr)
        return 1
    print(f"tokenizer: {'tiktoken o200k_base' if counter.exact else 'estimate (tiktoken unavailable)'}")
    total = 0
    for doc in docs:
        chunks = pipeline.prepare(doc)
        total += len(chunks)
        print(
            f"\n== {doc.document_id} ({doc.source_url}) language={chunks[0].language if chunks else '?'} "
            f"chunks={len(chunks)}"
        )
        for c in chunks:
            print(f"  [{c.chunk_index:02d}] {c.tokens:4d} tok  {c.heading_path}")
            print(f"        id={c.id}  sections={len(c.headings)}")
            if show_text:
                print("        " + c.content.replace("\n", "\n        "))
    print(f"\n{len(docs)} documents, {total} chunks (dry run: nothing was sent to Azure)")
    return 0


async def ingest(settings: Settings, root: Path, create_index: bool, force: bool) -> int:
    from azure.core.credentials import AzureKeyCredential
    from azure.core.credentials_async import AsyncTokenCredential
    from azure.identity.aio import DefaultAzureCredential
    from azure.search.documents.aio import SearchClient
    from azure.search.documents.indexes.aio import SearchIndexClient

    from ai_service.rag.index.schema import build_index
    from ai_service.rag.index.writer import AzureSearchIndexWriter

    if not (settings.search_endpoint and settings.search_index_name and settings.azure_openai_endpoint):
        print(
            "Set AZURE_OPENAI_ENDPOINT, SEARCH_ENDPOINT and SEARCH_INDEX_NAME (or use --dry-run)",
            file=sys.stderr,
        )
        return 2
    credential: AzureKeyCredential | AsyncTokenCredential = (
        AzureKeyCredential(settings.search_api_key.get_secret_value())
        if settings.search_api_key
        else DefaultAzureCredential()
    )
    if create_index:
        async with SearchIndexClient(settings.search_endpoint, credential) as index_client:
            await index_client.create_or_update_index(build_index(settings))
            print(f"index {settings.search_index_name}: created/updated")
    client = build_client(settings)
    gateway = LLMGateway(client, settings)
    async with SearchClient(settings.search_endpoint, settings.search_index_name, credential) as search:
        pipeline = build_pipeline(
            settings,
            TokenCounter(settings.tokenizer_mode),
            writer=AzureSearchIndexWriter(search),
            gateway=gateway,
        )
        for doc in repo_documents(root, settings):
            result = await pipeline.run(doc, force=force)
            state = (
                "unchanged"
                if result.skipped
                else f"{result.chunk_count} chunks, {result.embed_tokens} embed tokens"
            )
            print(f"{doc.document_id}: {state}")
    await client.close()
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--repo-root", type=Path, default=DEFAULT_ROOT)
    parser.add_argument("--dry-run", action="store_true", help="chunk and print, without Azure")
    parser.add_argument("--show-text", action="store_true", help="with --dry-run, print chunk text")
    parser.add_argument("--create-index", action="store_true", help="create or update the index schema first")
    parser.add_argument("--force", action="store_true", help="re-index even if the content is unchanged")
    args = parser.parse_args()
    settings = Settings()
    if args.dry_run:
        return dry_run(settings, args.repo_root, args.show_text)
    return asyncio.run(ingest(settings, args.repo_root, args.create_index, args.force))


if __name__ == "__main__":
    raise SystemExit(main())
