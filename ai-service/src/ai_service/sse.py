"""Server-sent event helpers."""

from __future__ import annotations

import json
from collections.abc import AsyncIterator
from typing import Any

from ai_service.api.errors import AppError, localize, to_app_error

SseEvent = dict[str, str]


def sse(event: str, payload: dict[str, Any]) -> SseEvent:
    return {"event": event, "data": json.dumps(payload, ensure_ascii=False)}


def sse_error(exc: BaseException, locale: str) -> SseEvent:
    err: AppError = to_app_error(exc)
    return sse("error", {"code": err.code, "message": localize(err.code, locale)})


async def prime(stream: AsyncIterator[SseEvent]) -> AsyncIterator[SseEvent]:
    """Run the stream up to its first event *before* the HTTP response starts.

    Auth, validation, rewrite, retrieval and opening the model stream all happen before the first
    event, so their errors still become proper HTTP statuses (400/429/503/504) instead of an SSE
    ``error`` event behind a 200.
    """
    first = await anext(stream)

    async def chained() -> AsyncIterator[SseEvent]:
        yield first
        async for event in stream:
            yield event

    return chained()
