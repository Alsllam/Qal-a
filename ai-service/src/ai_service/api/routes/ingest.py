"""Admin ingestion: ``POST /ai-api/ingest`` and ``GET /ai-api/ingest/{jobId}``."""

from __future__ import annotations

from typing import Annotated, Any, Literal

from fastapi import APIRouter, BackgroundTasks, Depends
from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel

from ai_service.api.deps import ContainerDep, require_permission
from ai_service.api.errors import AppError
from ai_service.auth import CurrentUser
from ai_service.rag.types import SourceDocument

router = APIRouter(prefix="/ai-api/ingest", tags=["ingest"])

IngestUser = Annotated[CurrentUser, Depends(require_permission("ingest_permission"))]


class IngestRequest(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="ignore")

    document_id: str = Field(min_length=1, max_length=200, pattern=r"^[A-Za-z0-9_.\-/]+$")
    title: str = Field(min_length=1, max_length=300)
    markdown: str = Field(min_length=1, max_length=2_000_000)
    doc_type: Literal["rules", "lesson", "strategy"] = "lesson"
    language: Literal["ar", "en"] | None = None
    acl_groups: list[str] = Field(default_factory=list, max_length=20)
    source_url: str = Field(default="", max_length=500)
    version: str = Field(default="", max_length=32)
    effective_date: str | None = Field(default=None, pattern=r"^\d{4}-\d{2}-\d{2}$")
    force: bool = False


@router.post("", status_code=202)
async def ingest(
    body: IngestRequest, user: IngestUser, container: ContainerDep, background: BackgroundTasks
) -> dict[str, Any]:
    doc = SourceDocument(
        tenant_id=user.tenant_id,  # from the token, never from the body
        document_id=body.document_id,
        title=body.title,
        markdown=body.markdown,
        acl_groups=tuple(body.acl_groups) or (container.settings.public_acl_group,),
        doc_type=body.doc_type,
        language=body.language,
        source_url=body.source_url,
        version=body.version,
        effective_date=body.effective_date,
    )
    job_id = await container.ingest.create_job(doc, requested_by=user.sub)
    background.add_task(container.ingest.run_job, job_id, doc, force=body.force)
    return {"jobId": job_id, "status": "queued"}


@router.get("/{job_id}")
async def get_job(job_id: str, user: IngestUser, container: ContainerDep) -> dict[str, Any]:
    job = await container.ingest.get_job(user.tenant_id, job_id)
    if job is None:
        raise AppError("General:Errors:NotFound", 404)
    return {
        "jobId": job.id,
        "documentId": job.document_id,
        "status": job.status,
        "chunkCount": job.chunk_count,
        "embedTokens": job.embed_tokens,
        "error": job.error,
        "createdAt": job.created_at.isoformat(),
        "finishedAt": job.finished_at.isoformat() if job.finished_at else None,
    }


@router.delete("/documents/{document_id:path}")
async def delete_document(document_id: str, user: IngestUser, container: ContainerDep) -> dict[str, Any]:
    deleted = await container.index_writer.delete_document(user.tenant_id, document_id)
    return {"documentId": document_id, "deletedChunks": deleted}
