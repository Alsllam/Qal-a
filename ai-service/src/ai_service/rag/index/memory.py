"""In-memory index with BM25 keyword retrieval.

Used for offline evals (CI runs without Azure), ``scripts/ingest_repo_docs.py --dry-run``,
tests, and local development with ``SEARCH_BACKEND=memory``. It applies the same security scope
as Azure AI Search.
"""

from __future__ import annotations

import math
import re
from collections import Counter
from typing import Any

from ai_service.rag.glossary import expand_arabic
from ai_service.rag.ingestion.normalize import normalize_for_search
from ai_service.rag.retrieval.security import ALLOWED_FILTER_FIELDS, SecurityScope
from ai_service.rag.types import RetrievedChunk, SearchQuery

_TOKEN = re.compile(r"\w+", re.UNICODE)
_STOP_EN = (
    "a an and are as at be by can do does for from has have how i if in is it its me my no not of on or so "
    "that the their them then there they this to was what when where which who why will with you your "
    "one any each only may must still more own"
)
_STOP_AR = (
    "في من على إلى الى عن ما ماذا هل كيف لماذا متى أن ان لا هو هي هذا "
    "هذه ذلك التي الذي او أو و ثم كم مع عند بعد قبل كل اي أي يمكن يجب لي"
)
_STOP = frozenset(_STOP_EN.split()) | frozenset(normalize_for_search(_STOP_AR).split())


def tokenize(text: str) -> list[str]:
    out: list[str] = []
    for raw in _TOKEN.findall(normalize_for_search(text).lower()):
        if raw in _STOP or (len(raw) < 2 and not raw.isdigit()):
            continue
        token = raw
        if token.startswith("ال") and len(token) > 4:
            token = token[2:]
        elif token.isascii() and len(token) > 4 and token.endswith("s") and not token.endswith("ss"):
            token = token[:-1]
        out.append(token)
    return out


class InMemoryIndex:
    """Implements ``IndexWriter`` and ``Retriever``."""

    def __init__(self, *, k1: float = 1.2, b: float = 0.75) -> None:
        self.docs: dict[str, dict[str, Any]] = {}
        self._k1 = k1
        self._b = b
        self._tf: dict[str, Counter[str]] = {}
        self._df: Counter[str] = Counter()
        self._avg_len = 0.0

    # ---- IndexWriter -------------------------------------------------------------------------
    async def current_sha(self, tenant_id: str, document_id: str) -> str | None:
        for d in self.docs.values():
            if d["tenant_id"] == tenant_id and d["document_id"] == document_id:
                return str(d["content_sha256"])
        return None

    async def upsert(self, documents: list[dict[str, Any]]) -> None:
        for d in documents:
            self.docs[d["id"]] = d
        self._reindex()

    async def delete_stale(self, tenant_id: str, document_id: str, keep_sha: str) -> int:
        stale = [
            k
            for k, d in self.docs.items()
            if d["tenant_id"] == tenant_id
            and d["document_id"] == document_id
            and d["content_sha256"] != keep_sha
        ]
        for k in stale:
            del self.docs[k]
        self._reindex()
        return len(stale)

    async def delete_document(self, tenant_id: str, document_id: str) -> int:
        doomed = [
            k for k, d in self.docs.items() if d["tenant_id"] == tenant_id and d["document_id"] == document_id
        ]
        for k in doomed:
            del self.docs[k]
        self._reindex()
        return len(doomed)

    # ---- BM25 ----------------------------------------------------------------------------------
    def _reindex(self) -> None:
        self._tf = {}
        self._df = Counter()
        for key, d in self.docs.items():
            # Headings count twice: they name what the chunk is about.
            text = " ".join([d["title"], *d.get("headings", []), *d.get("headings", []), d["content"]])
            tf = Counter(tokenize(text))
            self._tf[key] = tf
            self._df.update(tf.keys())
        lengths = [sum(tf.values()) for tf in self._tf.values()]
        self._avg_len = sum(lengths) / len(lengths) if lengths else 0.0

    def _score(self, key: str, terms: list[str]) -> float:
        tf = self._tf[key]
        length = sum(tf.values())
        n = len(self._tf)
        score = 0.0
        for term in set(terms):
            f = tf.get(term, 0)
            if not f:
                continue
            idf = math.log(1 + (n - self._df[term] + 0.5) / (self._df[term] + 0.5))
            norm = f + self._k1 * (1 - self._b + self._b * length / (self._avg_len or 1))
            score += idf * f * (self._k1 + 1) / norm
        return score

    async def retrieve(self, query: SearchQuery, scope: SecurityScope) -> list[RetrievedChunk]:
        text = " ".join([query.text, query.extra_keywords])
        if query.language == "ar" or not query.extra_keywords:
            text += " " + expand_arabic(query.text)
        terms = tokenize(text)
        for name in query.filters:
            if name not in ALLOWED_FILTER_FIELDS:
                raise ValueError(f"Filtering on '{name}' is not allowed")
        scored: list[tuple[float, str]] = []
        for key, d in self.docs.items():
            if not scope.allows(d["tenant_id"], d["acl_groups"]):
                continue
            if any(d.get(f) != v for f, v in query.filters.items()):
                continue
            s = self._score(key, terms)
            if s > 0:
                scored.append((s, key))
        scored.sort(key=lambda x: (-x[0], x[1]))
        return [_to_chunk(self.docs[k], s) for s, k in scored[: query.top]]

    async def ping(self) -> None:
        return None


def _to_chunk(d: dict[str, Any], score: float) -> RetrievedChunk:
    return RetrievedChunk(
        id=d["id"],
        document_id=d["document_id"],
        title=d["title"],
        heading_path=d["heading_path"],
        headings=tuple(d.get("headings", [])),
        content=d["content"],
        score=score,
        page=d.get("page"),
        source_url=d.get("source_url", ""),
        language=d.get("language", "en"),
    )
