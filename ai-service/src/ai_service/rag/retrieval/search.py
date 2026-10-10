"""Hybrid retrieval on Azure AI Search (BM25 + vector + semantic ranker) with security trimming."""

from __future__ import annotations

import re
from typing import Any, Protocol

from azure.search.documents.aio import SearchClient
from azure.search.documents.models import QueryCaptionType, QueryType, VectorizedQuery

from ai_service.rag.glossary import expand_arabic
from ai_service.rag.index.schema import SELECT_FIELDS
from ai_service.rag.ingestion.normalize import normalize_for_search
from ai_service.rag.ingestion.pipeline import Embedder
from ai_service.rag.retrieval.security import SecurityScope, build_filter
from ai_service.rag.types import RetrievedChunk, SearchQuery
from ai_service.settings import Settings

_WORDS = re.compile(r"\w+")


class Retriever(Protocol):
    async def retrieve(self, query: SearchQuery, scope: SecurityScope) -> list[RetrievedChunk]: ...
    async def ping(self) -> None: ...


def _shingles(text: str, n: int = 5) -> set[tuple[str, ...]]:
    words = _WORDS.findall(text.lower())
    return {tuple(words[i : i + n]) for i in range(max(1, len(words) - n + 1))}


def select_chunks(
    chunks: list[RetrievedChunk],
    *,
    threshold: float | None,
    per_document: int,
    limit: int,
    near_duplicate: float = 0.8,
) -> list[RetrievedChunk]:
    """Threshold on the reranker score, drop near-duplicates, cap chunks per document."""
    kept: list[RetrievedChunk] = []
    kept_shingles: list[set[tuple[str, ...]]] = []
    per_doc: dict[str, int] = {}
    for c in chunks:
        if threshold is not None and c.reranker_score is not None and c.reranker_score < threshold:
            continue
        if per_doc.get(c.document_id, 0) >= per_document:
            continue
        sh = _shingles(c.content)
        if any(len(sh & k) / max(1, len(sh | k)) >= near_duplicate for k in kept_shingles):
            continue
        kept.append(c)
        kept_shingles.append(sh)
        per_doc[c.document_id] = per_doc.get(c.document_id, 0) + 1
        if len(kept) >= limit:
            break
    return kept


class AzureHybridRetriever:
    def __init__(self, client: SearchClient, embedder: Embedder, settings: Settings) -> None:
        self.client = client
        self.embedder = embedder
        self.settings = settings

    async def retrieve(self, query: SearchQuery, scope: SecurityScope) -> list[RetrievedChunk]:
        keywords = query.extra_keywords or (expand_arabic(query.text) if query.language == "ar" else "")
        search_text = " ".join(p for p in (normalize_for_search(query.text), keywords) if p)
        vectors, _ = await self.embedder.embed([query.text])
        results = await self.client.search(
            search_text=search_text,
            filter=build_filter(scope, query.filters),  # security filter is always applied
            vector_queries=[
                VectorizedQuery(
                    vector=vectors[0],
                    k_nearest_neighbors=self.settings.search_vector_k,
                    fields="content_vector",
                )
            ],
            query_type=QueryType.SEMANTIC,
            semantic_configuration_name=self.settings.search_semantic_config,
            query_caption=QueryCaptionType.EXTRACTIVE,
            select=SELECT_FIELDS,
            top=self.settings.search_top,
        )
        raw: list[RetrievedChunk] = []
        async for r in results:
            raw.append(_from_result(r))
        return select_chunks(
            raw,
            threshold=self.settings.reranker_threshold,
            per_document=self.settings.max_chunks_per_document,
            limit=query.top,
        )

    async def ping(self) -> None:
        await self.client.get_document_count()


def _from_result(r: dict[str, Any]) -> RetrievedChunk:
    captions = r.get("@search.captions") or []
    caption = getattr(captions[0], "text", None) if captions else None
    return RetrievedChunk(
        id=str(r["id"]),
        document_id=str(r.get("document_id", "")),
        title=str(r.get("title", "")),
        heading_path=str(r.get("heading_path", "")),
        headings=tuple(r.get("headings") or ()),
        content=str(r.get("content", "")),
        score=float(r.get("@search.score") or 0.0),
        reranker_score=r.get("@search.reranker_score"),
        caption=caption,
        page=r.get("page"),
        source_url=str(r.get("source_url") or ""),
        language=str(r.get("language") or "en"),
    )
