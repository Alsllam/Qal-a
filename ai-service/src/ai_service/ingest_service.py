"""Ingestion jobs: record in SQL, run the pipeline, record the outcome.

Jobs run in-process (FastAPI background task). For large volumes, move ``run_job`` to an arq worker
fed by RabbitMQ ``DocumentUploaded`` events; the job record and the pipeline stay the same.
"""

from __future__ import annotations

import structlog
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from ai_service.conversations.models import IngestionJob, utcnow
from ai_service.rag.ingestion.pipeline import IngestionPipeline
from ai_service.rag.types import SourceDocument

log = structlog.get_logger(__name__)


class IngestService:
    def __init__(self, sessions: async_sessionmaker[AsyncSession], pipeline: IngestionPipeline) -> None:
        self.sessions = sessions
        self.pipeline = pipeline

    async def create_job(self, doc: SourceDocument, requested_by: str) -> str:
        async with self.sessions() as s, s.begin():
            job = IngestionJob(
                tenant_id=doc.tenant_id,
                requested_by=requested_by,
                document_id=doc.document_id,
                title=doc.title[:300],
                status="queued",
            )
            s.add(job)
            await s.flush()
            return job.id

    async def get_job(self, tenant_id: str, job_id: str) -> IngestionJob | None:
        async with self.sessions() as s:
            job = await s.get(IngestionJob, job_id)
            return job if job is not None and job.tenant_id == tenant_id else None

    async def _update(self, job_id: str, **values: object) -> None:
        async with self.sessions() as s, s.begin():
            job = await s.get(IngestionJob, job_id)
            if job is None:
                return
            for k, v in values.items():
                setattr(job, k, v)

    async def run_job(self, job_id: str, doc: SourceDocument, *, force: bool = False) -> None:
        await self._update(job_id, status="running")
        try:
            result = await self.pipeline.run(doc, force=force)
        except Exception as exc:
            log.exception("ingest.failed", job_id=job_id, error_type=type(exc).__name__)
            await self._update(job_id, status="failed", error=type(exc).__name__, finished_at=utcnow())
            return
        await self._update(
            job_id,
            status="skipped" if result.skipped else "succeeded",
            content_sha256=result.content_sha256,
            chunk_count=result.chunk_count,
            embed_tokens=result.embed_tokens,
            finished_at=utcnow(),
        )
