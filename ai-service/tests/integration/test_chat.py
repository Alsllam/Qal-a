import json
from typing import Any

import httpx
import respx
from httpx import AsyncClient

from ai_service.container import Container
from ai_service.llm.prompts import load_prompt
from ai_service.rag.retrieval.security import SecurityScope
from ai_service.rag.types import RetrievedChunk, SearchQuery
from tests.conftest import bearer
from tests.helpers import OPENAI_BASE, KeyPair, parse_sse, responses_json, responses_sse

SSE = {"content-type": "text/event-stream"}


def llm(mock_http: respx.MockRouter, deltas: list[str], rewrite: dict[str, Any] | None = None) -> respx.Route:
    def handler(request: httpx.Request) -> httpx.Response:
        body = json.loads(request.content)
        if "text" in body:  # structured output = query rewrite
            assert rewrite is not None, "unexpected rewrite call"
            return httpx.Response(200, json=responses_json(json.dumps(rewrite)))
        return httpx.Response(200, text=responses_sse(deltas), headers=SSE)

    return mock_http.post(f"{OPENAI_BASE}responses").mock(side_effect=handler)


def stream_calls(route: respx.Route) -> list[dict[str, Any]]:
    bodies = [json.loads(c.request.content) for c in route.calls]
    return [b for b in bodies if b.get("stream")]


async def test_chat_happy_path(api: AsyncClient, auth: dict[str, str], mock_http: respx.MockRouter) -> None:
    route = llm(mock_http, ["The Rami shoots exactly 2 squares away in a straight line", " [S1]."])
    r = await api.post(
        "/ai-api/chat", json={"message": "How does the Rami shoot?", "locale": "en"}, headers=auth
    )
    assert r.status_code == 200
    assert r.headers["content-type"].startswith("text/event-stream")
    events = parse_sse(r.text)
    kinds = [e for e, _ in events]
    assert kinds[0] == "meta" and kinds[-2:] == ["citations", "done"]
    assert "".join(d["text"] for e, d in events if e == "delta").endswith("[S1].")
    meta = events[0][1]
    assert meta["language"] == "en" and meta["promptVersion"] == "answer.v1" and meta["sources"] >= 1
    citations = dict(events)["citations"]["citations"]
    assert citations[0]["id"] == "S1" and citations[0]["documentId"] == "rules"
    done = dict(events)["done"]
    assert done["usage"] == {"inputTokens": 1200, "cachedTokens": 1024, "outputTokens": 40}

    (sent,) = stream_calls(route)
    assert sent["model"] == "chat-deployment"
    assert sent["store"] is False
    assert sent["instructions"] == load_prompt("answer", "v1")
    assert sent["input"][0]["role"] == "developer"
    assert '<source id="S1"' in sent["input"][0]["content"]
    assert "Rami" in sent["input"][0]["content"]
    assert sent["input"][-1] == {"role": "user", "content": "locale: en\n\nHow does the Rami shoot?"}

    # Conversation was stored, with resolved citations.
    conv_id = meta["conversationId"]
    stored = (await api.get(f"/ai-api/conversations/{conv_id}", headers=auth)).json()
    assert [m["role"] for m in stored["messages"]] == ["user", "assistant"]
    assert stored["messages"][1]["citations"][0]["id"] == "S1"


async def test_follow_up_uses_history_and_rewrite(
    api: AsyncClient, auth: dict[str, str], mock_http: respx.MockRouter
) -> None:
    rewrite = {
        "standalone_query": "Can the Rami shoot diagonally?",
        "search_query_en": "Rami shoot diagonally",
        "language": "en",
        "needs_retrieval": True,
        "topic": "rules",
    }
    route = llm(mock_http, ["No. It shoots straight [S1]."], rewrite)
    first = await api.post("/ai-api/chat", json={"message": "How does the Rami shoot?"}, headers=auth)
    conv_id = parse_sse(first.text)[0][1]["conversationId"]
    r = await api.post(
        "/ai-api/chat", json={"message": "And diagonally?", "conversationId": conv_id}, headers=auth
    )
    assert r.status_code == 200
    bodies = [json.loads(c.request.content) for c in route.calls]
    rewrite_call = next(b for b in bodies if "text" in b)
    assert rewrite_call["model"] == "fast-deployment"
    assert "How does the Rami shoot?" in rewrite_call["input"][0]["content"]
    last = stream_calls(route)[-1]
    roles = [i["role"] for i in last["input"]]
    assert roles == ["developer", "user", "assistant", "user"]


async def test_arabic_question_is_rewritten_and_answered_in_arabic(
    api: AsyncClient, auth: dict[str, str], mock_http: respx.MockRouter
) -> None:
    rewrite = {
        "standalone_query": "كيف يرمي الرامي؟",
        "search_query_en": "Rami shoot straight 2 squares",
        "language": "ar",
        "needs_retrieval": True,
        "topic": "rules",
    }
    llm(mock_http, ["يرمي الرامي قطعة على بعد مربعين في خط مستقيم [S1]."], rewrite)
    r = await api.post("/ai-api/chat", json={"message": "كيف يرمي الرامي؟", "locale": "ar"}, headers=auth)
    events = parse_sse(r.text)
    assert events[0][1]["language"] == "ar"
    assert dict(events)["citations"]["citations"]


async def test_no_sources_means_no_model_call(
    api: AsyncClient, auth: dict[str, str], mock_http: respx.MockRouter
) -> None:
    route = llm(mock_http, ["should not be used"])
    r = await api.post("/ai-api/chat", json={"message": "What's the capital of France?"}, headers=auth)
    events = parse_sse(r.text)
    assert dict(events)["done"]["noAnswer"] is True
    assert "official Qal'a rules" in "".join(d["text"] for e, d in events if e == "delta")
    assert route.call_count == 0


async def test_content_filter_on_prompt_returns_400_localized(
    api: AsyncClient, auth: dict[str, str], mock_http: respx.MockRouter
) -> None:
    mock_http.post(f"{OPENAI_BASE}responses").mock(
        return_value=httpx.Response(
            400,
            json={"error": {"code": "content_filter", "message": "filtered", "type": None, "param": None}},
        )
    )
    r = await api.post(
        "/ai-api/chat",
        json={"message": "How does the Faris move?"},
        headers={**auth, "Accept-Language": "ar"},
    )
    assert r.status_code == 400
    error = r.json()["error"]
    assert error["code"] == "General:Errors:ContentBlocked"
    assert error["source"] == "Ai"
    assert error["messages"][0].startswith("تعذّرت")


async def test_rate_limited_upstream_returns_503_ai_busy(
    api: AsyncClient, auth: dict[str, str], mock_http: respx.MockRouter
) -> None:
    route = mock_http.post(f"{OPENAI_BASE}responses").mock(
        return_value=httpx.Response(429, headers={"retry-after": "0"}, json={})
    )
    r = await api.post("/ai-api/chat", json={"message": "How does the Faris move?"}, headers=auth)
    assert r.status_code == 503
    assert r.json()["error"]["code"] == "General:Errors:AiBusy"
    assert route.call_count == 4


async def test_mid_stream_error_sends_error_event_and_keeps_partial(
    api: AsyncClient, auth: dict[str, str], mock_http: respx.MockRouter
) -> None:
    mock_http.post(f"{OPENAI_BASE}responses").mock(
        return_value=httpx.Response(200, text=responses_sse(["Partial"], fail="server_error"), headers=SSE)
    )
    r = await api.post("/ai-api/chat", json={"message": "How does the Faris move?"}, headers=auth)
    events = parse_sse(r.text)
    assert [e for e, _ in events][-3:] == ["error", "citations", "done"]
    assert dict(events)["done"]["incomplete"] is True
    conv = (await api.get(f"/ai-api/conversations/{events[0][1]['conversationId']}", headers=auth)).json()
    assert conv["messages"][1] == {**conv["messages"][1], "content": "Partial", "incomplete": True}


async def test_security_scope_comes_from_token_not_body(
    api: AsyncClient, keys: KeyPair, container: Container, mock_http: respx.MockRouter
) -> None:
    seen: list[SecurityScope] = []
    inner = container.retriever

    class Spy:
        async def retrieve(self, query: SearchQuery, scope: SecurityScope) -> list[RetrievedChunk]:
            seen.append(scope)
            return await inner.retrieve(query, scope)

        async def ping(self) -> None:
            return None

    container.retriever = Spy()
    llm(mock_http, ["ok [S1]"])
    body = {
        "message": "How does the Faris move?",
        "tenantId": "other-tenant",
        "tenant_id": "other-tenant",
        "filter": "tenant_id eq 'other-tenant'",
        "aclGroups": ["admins"],
        "context": {"screen": "rules", "tenantId": "other-tenant"},
    }
    r = await api.post("/ai-api/chat", json=body, headers=bearer(keys, tenant_id="qala", role=["player"]))
    assert r.status_code == 200
    assert seen == [SecurityScope(tenant_id="qala", groups=("public", "player"))]


async def test_conversations_are_private_and_deletable(
    api: AsyncClient, keys: KeyPair, mock_http: respx.MockRouter
) -> None:
    llm(mock_http, ["ok [S1]"])
    owner = bearer(keys, sub="owner")
    r = await api.post("/ai-api/chat", json={"message": "How does the Faris move?"}, headers=owner)
    conv_id = parse_sse(r.text)[0][1]["conversationId"]
    listed = (await api.get("/ai-api/conversations", headers=owner)).json()
    assert listed["totalCount"] == 1 and listed["items"][0]["id"] == conv_id
    other = bearer(keys, sub="someone-else")
    assert (await api.get(f"/ai-api/conversations/{conv_id}", headers=other)).status_code == 404
    assert (await api.delete(f"/ai-api/conversations/{conv_id}", headers=other)).status_code == 404
    assert (await api.delete(f"/ai-api/conversations/{conv_id}", headers=owner)).status_code == 204
    assert (await api.get(f"/ai-api/conversations/{conv_id}", headers=owner)).status_code == 404
    unknown = await api.post(
        "/ai-api/chat", json={"message": "hi there", "conversationId": conv_id}, headers=owner
    )
    assert unknown.status_code == 404
