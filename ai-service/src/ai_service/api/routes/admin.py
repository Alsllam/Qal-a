"""Admin: usage summary and index schema management."""

from __future__ import annotations

from datetime import UTC, datetime, timedelta
from typing import Annotated, Any

from fastapi import APIRouter, Depends, Query

from ai_service.api.deps import ContainerDep, require_permission
from ai_service.api.errors import AppError
from ai_service.auth import CurrentUser

router = APIRouter(prefix="/ai-api/admin", tags=["admin"])

AdminUser = Annotated[CurrentUser, Depends(require_permission("admin_permission"))]
IngestUser = Annotated[CurrentUser, Depends(require_permission("ingest_permission"))]


@router.get("/usage")
async def usage(
    user: AdminUser, container: ContainerDep, days: int = Query(default=7, ge=1, le=90)
) -> dict[str, Any]:
    since = datetime.now(UTC) - timedelta(days=days)
    rows = await container.meter.summary(user.tenant_id, since)
    return {
        "since": since.isoformat(),
        "items": [
            {
                "feature": r.feature,
                "deployment": r.deployment,
                "requests": r.requests,
                "inputTokens": r.input_tokens,
                "cachedTokens": r.cached_tokens,
                "outputTokens": r.output_tokens,
                "costUsd": round(r.cost_usd, 4),
            }
            for r in rows
        ],
    }


@router.post("/index")
async def create_or_update_index(user: IngestUser, container: ContainerDep) -> dict[str, Any]:
    """Create or update the Azure AI Search index from ``rag/index/schema.py``."""
    settings = container.settings
    if settings.search_backend != "azure":
        raise AppError("General:Errors:NotFound", 404)
    from azure.core.credentials import AzureKeyCredential
    from azure.core.credentials_async import AsyncTokenCredential
    from azure.identity.aio import DefaultAzureCredential
    from azure.search.documents.indexes.aio import SearchIndexClient

    from ai_service.rag.index.schema import build_index

    credential: AzureKeyCredential | AsyncTokenCredential = (
        AzureKeyCredential(settings.search_api_key.get_secret_value())
        if settings.search_api_key
        else DefaultAzureCredential()
    )
    async with SearchIndexClient(settings.search_endpoint, credential) as client:
        index = await client.create_or_update_index(build_index(settings))
    return {"index": index.name, "fields": len(index.fields)}


@router.post("/retention")
async def purge(user: AdminUser, container: ContainerDep) -> dict[str, Any]:
    """Delete conversations older than RETENTION_DAYS (also run on a schedule)."""
    deleted = await container.conversations.purge_older_than(container.settings.retention_days)
    return {"deletedConversations": deleted}
