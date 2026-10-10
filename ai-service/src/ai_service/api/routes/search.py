"""``POST /ai-api/search``: retrieval only (in-app rules search), no generation."""

from __future__ import annotations

from typing import Any, Literal

from fastapi import APIRouter
from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel

from ai_service.api.deps import ContainerDep, LimitedUserDep, ScopeDep
from ai_service.rag.glossary import expand_arabic
from ai_service.rag.ingestion.normalize import detect_language
from ai_service.rag.types import SearchQuery

router = APIRouter(prefix="/ai-api", tags=["search"])


class SearchRequest(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="ignore")

    query: str = Field(min_length=1, max_length=500)
    top: int = Field(default=5, ge=1, le=20)
    doc_type: Literal["rules", "lesson", "strategy"] | None = None


@router.post("/search")
async def search(
    body: SearchRequest, user: LimitedUserDep, scope: ScopeDep, container: ContainerDep
) -> dict[str, Any]:
    language = detect_language(body.query)
    chunks = await container.retriever.retrieve(
        SearchQuery(
            text=body.query,
            language=language,
            extra_keywords=expand_arabic(body.query) if language == "ar" else "",
            top=body.top,
            filters={"doc_type": body.doc_type} if body.doc_type else {},
        ),
        scope,
    )
    return {
        "items": [
            {
                "id": c.id,
                "documentId": c.document_id,
                "title": c.title,
                "headingPath": c.heading_path,
                "snippet": c.caption or c.content[:400],
                "content": c.content,
                "page": c.page,
                "url": c.source_url or None,
                "score": c.reranker_score if c.reranker_score is not None else c.score,
            }
            for c in chunks
        ]
    }
