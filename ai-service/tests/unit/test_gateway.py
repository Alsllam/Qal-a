import json

import httpx
import openai
import pytest
import respx
from openai import AsyncOpenAI

from ai_service.llm.gateway import LLMGateway, StreamDone, StreamFailedError, TextDelta
from ai_service.llm.schemas import QueryRewrite, strict_json_schema
from ai_service.settings import ModelRole, Settings
from tests.helpers import OPENAI_BASE, responses_json, responses_sse


@pytest.fixture
def gateway(settings: Settings) -> LLMGateway:
    return LLMGateway(
        AsyncOpenAI(base_url=OPENAI_BASE, api_key="k", max_retries=0), settings, retry_wait_scale=0
    )


def test_strict_schema_requires_all_fields_and_forbids_extras() -> None:
    schema = strict_json_schema(QueryRewrite)
    assert schema["additionalProperties"] is False
    assert set(schema["required"]) == set(schema["properties"])


async def test_parse_retries_429_then_succeeds(gateway: LLMGateway, mock_http: respx.MockRouter) -> None:
    payload = {
        "standalone_query": "How does the Faris move?",
        "search_query_en": "Faris move",
        "language": "en",
        "needs_retrieval": True,
        "topic": "rules",
    }
    route = mock_http.post(f"{OPENAI_BASE}responses").mock(
        side_effect=[
            httpx.Response(429, headers={"retry-after": "0"}, json={"error": {"code": "429"}}),
            httpx.Response(200, json=responses_json(json.dumps(payload))),
        ]
    )
    result, usage = await gateway.parse(ModelRole.FAST, QueryRewrite, instructions="x", input_items=[])
    assert result.search_query_en == "Faris move"
    assert route.call_count == 2
    sent = json.loads(route.calls[-1].request.content)
    assert sent["model"] == "fast-deployment"
    assert sent["store"] is False
    assert sent["text"]["format"]["strict"] is True
    assert usage.input_tokens == 50


async def test_rate_limit_gives_up_after_max_attempts(
    gateway: LLMGateway, mock_http: respx.MockRouter
) -> None:
    route = mock_http.post(f"{OPENAI_BASE}responses").mock(return_value=httpx.Response(429, json={}))
    with pytest.raises(openai.RateLimitError):
        await gateway.parse(ModelRole.FAST, QueryRewrite, instructions="x", input_items=[])
    assert route.call_count == 4


async def test_stream_does_not_retry_timeouts(gateway: LLMGateway, mock_http: respx.MockRouter) -> None:
    route = mock_http.post(f"{OPENAI_BASE}responses").mock(side_effect=httpx.ReadTimeout("slow"))
    with pytest.raises(openai.APITimeoutError):
        await gateway.open_text_stream(ModelRole.CHAT, instructions="x", input_items=[], max_output_tokens=10)
    assert route.call_count == 1


async def test_stream_yields_deltas_and_usage(gateway: LLMGateway, mock_http: respx.MockRouter) -> None:
    mock_http.post(f"{OPENAI_BASE}responses").mock(
        return_value=httpx.Response(
            200, text=responses_sse(["Hel", "lo"]), headers={"content-type": "text/event-stream"}
        )
    )
    stream = await gateway.open_text_stream(
        ModelRole.CHAT, instructions="x", input_items=[], max_output_tokens=10
    )
    events = [e async for e in stream]
    assert [e.text for e in events if isinstance(e, TextDelta)] == ["Hel", "lo"]
    done = events[-1]
    assert isinstance(done, StreamDone)
    assert (done.usage.input_tokens, done.usage.cached_tokens, done.usage.output_tokens) == (1200, 1024, 40)


async def test_mid_stream_error_event_raises(gateway: LLMGateway, mock_http: respx.MockRouter) -> None:
    mock_http.post(f"{OPENAI_BASE}responses").mock(
        return_value=httpx.Response(
            200,
            text=responses_sse(["partial"], fail="content_filter"),
            headers={"content-type": "text/event-stream"},
        )
    )
    stream = await gateway.open_text_stream(
        ModelRole.CHAT, instructions="x", input_items=[], max_output_tokens=10
    )
    seen: list[str] = []
    with pytest.raises(StreamFailedError) as info:
        async for e in stream:
            if isinstance(e, TextDelta):
                seen.append(e.text)
    assert seen == ["partial"]
    assert info.value.code == "content_filter"


def test_missing_deployment_is_a_configuration_error(gateway: LLMGateway) -> None:
    with pytest.raises(RuntimeError, match="DEPLOYMENTS__REASONING"):
        gateway.deployment(ModelRole.REASONING)
