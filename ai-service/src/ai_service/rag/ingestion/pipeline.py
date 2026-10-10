"""Ingestion: Markdown → normalize → chunk → enrich → embed → upsert → delete stale chunks.

Idempotent: the key is ``document_id`` + the SHA-256 of the content; unchanged content is skipped.
"""

from __future__ import annotations

import hashlib
import re
from collections.abc import Sequence
from dataclasses import dataclass
from typing import Any, Protocol

import structlog

from ai_service.llm.gateway import LLMUsage
from ai_service.rag.ingestion.chunker import MarkdownChunker
from ai_service.rag.ingestion.normalize import detect_language, normalize_for_search
from ai_service.rag.types import IndexChunk, SourceDocument

log = structlog.get_logger(__name__)

_KEY_SAFE = re.compile(r"[^A-Za-z0-9_\-=]")


class Embedder(Protocol):
    async def embed(
        self, texts: Sequence[str], *, batch_size: int = 64
    ) -> tuple[list[list[float]], LLMUsage]: ...


class IndexWriter(Protocol):
    async def current_sha(self, tenant_id: str, document_id: str) -> str | None: ...
    async def upsert(self, documents: list[dict[str, Any]]) -> None: ...
    async def delete_stale(self, tenant_id: str, document_id: str, keep_sha: str) -> int: ...
    async def delete_document(self, tenant_id: str, document_id: str) -> int: ...


@dataclass(frozen=True)
class IngestResult:
    document_id: str
    content_sha256: str
    chunk_count: int
    skipped: bool
    embed_tokens: int = 0
    deleted_stale: int = 0


def content_sha256(doc: SourceDocument) -> str:
    h = hashlib.sha256()
    for part in (doc.title, doc.markdown, ",".join(sorted(doc.acl_groups)), doc.version):
        h.update(part.encode("utf-8"))
        h.update(b"\x00")
    return h.hexdigest()


def chunk_key(document_id: str, sha: str, index: int) -> str:
    return f"{_KEY_SAFE.sub('_', document_id)}-{sha[:12]}-{index:04d}"


class IngestionPipeline:
    def __init__(
        self,
        chunker: MarkdownChunker,
        *,
        product: str,
        embedder: Embedder | None = None,
        writer: IndexWriter | None = None,
    ) -> None:
        self.chunker = chunker
        self.product = product
        self.embedder = embedder
        self.writer = writer

    def prepare(self, doc: SourceDocument) -> list[IndexChunk]:
        """Pure step (no network): chunk and enrich. Used by dry-run, evals and tests."""
        sha = content_sha256(doc)
        language = doc.language or detect_language(doc.markdown)
        out: list[IndexChunk] = []
        for chunk in self.chunker.chunk(doc.markdown):
            out.append(
                IndexChunk(
                    id=chunk_key(doc.document_id, sha, chunk.index),
                    tenant_id=doc.tenant_id,
                    product=self.product,
                    document_id=doc.document_id,
                    title=doc.title,
                    heading_path=chunk.heading_path,
                    headings=tuple(chunk.headings),
                    content=chunk.text,
                    content_search=normalize_for_search(chunk.text),
                    language=language,
                    acl_groups=doc.acl_groups,
                    doc_type=doc.doc_type,
                    source_url=doc.source_url,
                    version=doc.version,
                    effective_date=doc.effective_date,
                    chunk_index=chunk.index,
                    content_sha256=sha,
                    tokens=chunk.tokens,
                )
            )
        return out

    async def run(self, doc: SourceDocument, *, force: bool = False) -> IngestResult:
        if self.writer is None:
            raise RuntimeError("No index writer configured")
        sha = content_sha256(doc)
        if not force and await self.writer.current_sha(doc.tenant_id, doc.document_id) == sha:
            log.info("ingest.unchanged", document_id=doc.document_id)
            return IngestResult(doc.document_id, sha, 0, skipped=True)
        chunks = self.prepare(doc)
        vectors: list[list[float]] | list[None] = [None] * len(chunks)
        embed_tokens = 0
        if self.embedder is not None and chunks:
            # Embed the heading path with the text: it disambiguates short sections.
            texts = [f"{c.title}\n{c.heading_path}\n\n{c.content}" for c in chunks]
            vectors, usage = await self.embedder.embed(texts)
            embed_tokens = usage.input_tokens
        await self.writer.upsert([c.to_document(v) for c, v in zip(chunks, vectors, strict=True)])
        deleted = await self.writer.delete_stale(doc.tenant_id, doc.document_id, keep_sha=sha)
        log.info(
            "ingest.indexed",
            document_id=doc.document_id,
            chunks=len(chunks),
            deleted_stale=deleted,
            embed_tokens=embed_tokens,
        )
        return IngestResult(doc.document_id, sha, len(chunks), False, embed_tokens, deleted)
