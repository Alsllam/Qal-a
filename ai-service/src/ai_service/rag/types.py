"""Data passed between ingestion, the index and retrieval."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(frozen=True)
class SourceDocument:
    """A document to index (Markdown text; PDFs go through Document Intelligence first)."""

    tenant_id: str
    document_id: str
    title: str
    markdown: str
    acl_groups: tuple[str, ...]
    doc_type: str = "rules"
    language: str | None = None
    source_url: str = ""
    version: str = ""
    effective_date: str | None = None  # ISO-8601 date


@dataclass(frozen=True)
class IndexChunk:
    """One chunk, enriched and ready to embed and upsert."""

    id: str
    tenant_id: str
    product: str
    document_id: str
    title: str
    heading_path: str
    headings: tuple[str, ...]
    content: str
    content_search: str
    language: str
    acl_groups: tuple[str, ...]
    doc_type: str
    source_url: str
    version: str
    effective_date: str | None
    chunk_index: int
    content_sha256: str
    tokens: int
    page: int | None = None
    summary: str = ""

    def to_document(self, vector: list[float] | None) -> dict[str, Any]:
        doc: dict[str, Any] = {
            "id": self.id,
            "tenant_id": self.tenant_id,
            "product": self.product,
            "document_id": self.document_id,
            "title": self.title,
            "heading_path": self.heading_path,
            "headings": list(self.headings),
            "summary": self.summary,
            "content": self.content,
            "content_ar": self.content if self.language == "ar" else "",
            "content_en": self.content if self.language != "ar" else "",
            "content_search": self.content_search,
            "page": self.page,
            "language": self.language,
            "acl_groups": list(self.acl_groups),
            "doc_type": self.doc_type,
            "effective_date": f"{self.effective_date}T00:00:00Z" if self.effective_date else None,
            "version": self.version,
            "source_url": self.source_url,
            "chunk_index": self.chunk_index,
            "content_sha256": self.content_sha256,
        }
        if vector is not None:
            doc["content_vector"] = vector
        return doc


@dataclass(frozen=True)
class RetrievedChunk:
    id: str
    document_id: str
    title: str
    heading_path: str
    content: str
    score: float
    headings: tuple[str, ...] = ()
    page: int | None = None
    source_url: str = ""
    reranker_score: float | None = None
    caption: str | None = None
    language: str = "en"


@dataclass(frozen=True)
class SearchQuery:
    text: str
    language: str = "en"
    extra_keywords: str = ""
    top: int = 5
    filters: dict[str, str] = field(default_factory=dict)  # whitelisted fields only, see security.py
