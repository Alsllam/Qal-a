"""Auth, rate limit, search, ingest, admin and health through the HTTP API."""

import time

import httpx
import respx
from httpx import AsyncClient

from ai_service.container import Container
from ai_service.rag.types import IndexChunk
from tests.conftest import bearer
from tests.helpers import OPENAI_BASE, KeyPair

INGEST = "Permissions.Content.ManageLessons"


async def test_missing_token_is_401(api: AsyncClient) -> None:
    r = await api.post("/ai-api/search", json={"query": "water"})
    assert r.status_code == 401
    assert r.json()["error"]["code"] == "General:Errors:Unauthorized"
    assert r.headers["www-authenticate"].startswith("Bearer")


async def test_wrong_audience_and_expired_tokens_are_401(api: AsyncClient, keys: KeyPair) -> None:
    r = await api.post("/ai-api/search", json={"query": "water"}, headers=bearer(keys, aud="matches-api"))
    assert r.status_code == 401
    past = int(time.time()) - 7200
    r = await api.post(
        "/ai-api/search", json={"query": "water"}, headers=bearer(keys, iat=past, exp=past + 60)
    )
    assert r.status_code == 401


async def test_rate_limit_returns_429_with_retry_after(api: AsyncClient, keys: KeyPair) -> None:
    headers = bearer(keys, sub="fast-typer")
    codes = [
        (await api.post("/ai-api/search", json={"query": "water"}, headers=headers)).status_code
        for _ in range(6)
    ]
    assert codes == [200] * 5 + [429]
    r = await api.post(
        "/ai-api/search", json={"query": "water"}, headers={**headers, "Accept-Language": "ar"}
    )
    assert r.status_code == 429
    assert int(r.headers["retry-after"]) >= 1
    assert r.json()["error"]["code"] == "General:Errors:TooManyRequests"
    assert r.json()["error"]["messages"][0].startswith("أسئلتك")
    # Another user is not affected.
    assert (
        await api.post("/ai-api/search", json={"query": "water"}, headers=bearer(keys, sub="b"))
    ).status_code == 200


async def test_search_returns_rules_sections(api: AsyncClient, auth: dict[str, str]) -> None:
    r = await api.post(
        "/ai-api/search", json={"query": "Can the Rami shoot diagonally?", "top": 3}, headers=auth
    )
    items = r.json()["items"]
    assert items and items[0]["documentId"] == "rules"
    assert any("Rami" in i["content"] for i in items)


async def test_search_cannot_reach_other_tenants(
    api: AsyncClient, keys: KeyPair, container: Container
) -> None:
    secret = IndexChunk(
        id="secret-1",
        tenant_id="other-tenant",
        product="qala",
        document_id="secret",
        title="Secret",
        heading_path="Secret",
        headings=("Secret",),
        content="Rami Rami Rami secret strategy",
        content_search="rami secret",
        language="en",
        acl_groups=("public",),
        doc_type="rules",
        source_url="",
        version="",
        effective_date=None,
        chunk_index=0,
        content_sha256="x",
        tokens=5,
    )
    await container.index_writer.upsert([secret.to_document(None)])
    body = {
        "query": "Rami secret strategy",
        "tenantId": "other-tenant",
        "filter": "tenant_id eq 'other-tenant'",
    }
    r = await api.post("/ai-api/search", json=body, headers=bearer(keys, tenant_id="qala"))
    assert all(i["documentId"] != "secret" for i in r.json()["items"])
    r = await api.post("/ai-api/search", json=body, headers=bearer(keys, tenant_id="other-tenant"))
    assert r.json()["items"][0]["documentId"] == "secret"


async def test_ingest_requires_permission(api: AsyncClient, auth: dict[str, str]) -> None:
    r = await api.post(
        "/ai-api/ingest", json={"documentId": "x", "title": "x", "markdown": "# x"}, headers=auth
    )
    assert r.status_code == 403
    assert r.json()["error"]["code"] == "General:Errors:Forbidden"


async def test_ingest_job_indexes_and_is_idempotent(api: AsyncClient, keys: KeyPair) -> None:
    admin = bearer(keys, permission=[INGEST])
    lesson = {
        "documentId": "lessons/01-water",
        "title": "Lesson 1: Water",
        "markdown": "# Lesson 1: Water\n\n## Keep the chain\n\nA cut-off Faris can walk but cannot capture. "
        "Reconnect it to the Amir before attacking the oasis caravan.",
        "docType": "lesson",
        "tenantId": "ignored-from-body",
    }
    r = await api.post("/ai-api/ingest", json=lesson, headers=admin)
    assert r.status_code == 202
    job = (await api.get(f"/ai-api/ingest/{r.json()['jobId']}", headers=admin)).json()
    assert job["status"] == "succeeded" and job["chunkCount"] == 1
    found = (await api.post("/ai-api/search", json={"query": "oasis caravan"}, headers=admin)).json()["items"]
    assert found[0]["documentId"] == "lessons/01-water"
    again = await api.post("/ai-api/ingest", json=lesson, headers=admin)
    job2 = (await api.get(f"/ai-api/ingest/{again.json()['jobId']}", headers=admin)).json()
    assert job2["status"] == "skipped"
    deleted = await api.delete("/ai-api/ingest/documents/lessons/01-water", headers=admin)
    assert deleted.json() == {"documentId": "lessons/01-water", "deletedChunks": 1}
    found = (await api.post("/ai-api/search", json={"query": "oasis caravan"}, headers=admin)).json()["items"]
    assert all(i["documentId"] != "lessons/01-water" for i in found)


async def test_validation_errors_use_backend_shape(api: AsyncClient, auth: dict[str, str]) -> None:
    r = await api.post("/ai-api/chat", json={"message": ""}, headers=auth)
    assert r.status_code == 400
    err = r.json()["error"]
    assert err["code"] == "General:Errors:Validation" and len(err["messages"]) >= 2


async def test_health(api: AsyncClient, mock_http: respx.MockRouter) -> None:
    assert (await api.get("/health/live")).json() == {"status": "ok"}
    mock_http.get(f"{OPENAI_BASE}models").mock(
        return_value=httpx.Response(200, json={"object": "list", "data": []})
    )
    r = await api.get("/health/ready")
    assert r.status_code == 200, r.text
    assert r.json()["checks"] == {"database": "ok", "search": "ok", "openai": "ok"}


async def test_ready_fails_when_openai_is_down(api: AsyncClient, mock_http: respx.MockRouter) -> None:
    mock_http.get(f"{OPENAI_BASE}models").mock(return_value=httpx.Response(401, json={}))
    r = await api.get("/health/ready")
    assert r.status_code == 503
    assert r.json()["checks"]["openai"].startswith("fail")


async def test_usage_is_metered(api: AsyncClient, keys: KeyPair, mock_http: respx.MockRouter) -> None:
    from tests.helpers import responses_sse

    mock_http.post(f"{OPENAI_BASE}responses").mock(
        return_value=httpx.Response(
            200, text=responses_sse(["ok"]), headers={"content-type": "text/event-stream"}
        )
    )
    await api.post("/ai-api/chat", json={"message": "How does the Faris move?"}, headers=bearer(keys))
    admin = bearer(keys, sub="admin", permission=["Permissions.Dashboard.ViewBalance"])
    items = (await api.get("/ai-api/admin/usage", headers=admin)).json()["items"]
    assert items == [
        {
            "feature": "chat.answer",
            "deployment": "chat-deployment",
            "requests": 1,
            "inputTokens": 1200,
            "cachedTokens": 1024,
            "outputTokens": 40,
            "costUsd": 0.0,
        }
    ]
