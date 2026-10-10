"""Azure AI Search writer (upsert, stale-version cleanup, document delete)."""

from __future__ import annotations

from typing import Any

from azure.search.documents.aio import SearchClient

from ai_service.rag.retrieval.security import odata_literal


def _doc_filter(tenant_id: str, document_id: str) -> str:
    return f"tenant_id eq {odata_literal(tenant_id)} and document_id eq {odata_literal(document_id)}"


class AzureSearchIndexWriter:
    def __init__(self, client: SearchClient) -> None:
        self.client = client

    async def _keys(self, filter_: str) -> list[str]:
        results = await self.client.search(search_text="*", filter=filter_, select=["id"], top=1000)
        return [str(r["id"]) async for r in results]

    async def current_sha(self, tenant_id: str, document_id: str) -> str | None:
        results = await self.client.search(
            search_text="*",
            filter=_doc_filter(tenant_id, document_id),
            select=["content_sha256"],
            top=1,
        )
        async for r in results:
            return str(r["content_sha256"])
        return None

    async def upsert(self, documents: list[dict[str, Any]]) -> None:
        for start in range(0, len(documents), 500):
            results = await self.client.merge_or_upload_documents(documents[start : start + 500])
            failed = [r.key for r in results if not r.succeeded]
            if failed:
                raise RuntimeError(f"Index upsert failed for {len(failed)} chunks")

    async def _delete(self, filter_: str) -> int:
        keys = await self._keys(filter_)
        if keys:
            await self.client.delete_documents([{"id": k} for k in keys])
        return len(keys)

    async def delete_stale(self, tenant_id: str, document_id: str, keep_sha: str) -> int:
        return await self._delete(
            f"{_doc_filter(tenant_id, document_id)} and content_sha256 ne {odata_literal(keep_sha)}"
        )

    async def delete_document(self, tenant_id: str, document_id: str) -> int:
        return await self._delete(_doc_filter(tenant_id, document_id))
