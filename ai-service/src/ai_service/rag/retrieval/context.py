"""Context assembly: fill a token budget with the best chunks, tagged ``<source id="S#">``."""

from __future__ import annotations

import re
from dataclasses import dataclass, field

from ai_service.rag.types import RetrievedChunk
from ai_service.safety.injection import wrap_source
from ai_service.tokens import TokenCounter

_CITATION = re.compile(r"\[(S\d+)\]")


@dataclass(frozen=True)
class Citation:
    source_id: str
    chunk_id: str
    document_id: str
    title: str
    heading_path: str
    page: int | None
    url: str

    def to_public(self) -> dict[str, object]:
        return {
            "id": self.source_id,
            "documentId": self.document_id,
            "title": self.title,
            "headingPath": self.heading_path,
            "page": self.page,
            "url": self.url or None,
        }


@dataclass
class ContextBundle:
    text: str
    citations: dict[str, Citation] = field(default_factory=dict)
    tokens: int = 0
    dropped: int = 0

    @property
    def empty(self) -> bool:
        return not self.citations


def assemble_context(
    chunks: list[RetrievedChunk],
    counter: TokenCounter,
    budget_tokens: int,
    *,
    start_index: int = 1,
) -> ContextBundle:
    """Add chunks in rank order while they fit; a chunk that does not fit is skipped."""
    parts: list[str] = []
    citations: dict[str, Citation] = {}
    used = 0
    dropped = 0
    n = start_index
    for chunk in chunks:
        sid = f"S{n}"
        block = wrap_source(sid, chunk.content, title=chunk.title, path=chunk.heading_path, page=chunk.page)
        tokens = counter.count(block)
        if used + tokens > budget_tokens:
            dropped += 1
            continue
        parts.append(block)
        used += tokens
        citations[sid] = Citation(
            source_id=sid,
            chunk_id=chunk.id,
            document_id=chunk.document_id,
            title=chunk.title,
            heading_path=chunk.heading_path,
            page=chunk.page,
            url=chunk.source_url,
        )
        n += 1
    return ContextBundle(text="\n\n".join(parts), citations=citations, tokens=used, dropped=dropped)


def extract_citations(answer: str, citations: dict[str, Citation]) -> list[Citation]:
    """Citations used in the answer, in order of first use; unknown ids are ignored."""
    seen: list[str] = []
    for sid in _CITATION.findall(answer):
        if sid in citations and sid not in seen:
            seen.append(sid)
    return [citations[s] for s in seen]
