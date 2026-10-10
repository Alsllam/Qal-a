"""The single place that calls Azure OpenAI (Responses + Embeddings), with one retry policy.

* Rate limits (429) and 5xx: exponential backoff with jitter, honoring ``Retry-After``.
* Timeouts/connection errors: retried only for idempotent calls (embeddings, structured
  classification). Streaming chat fails fast so the client can retry.
* A stream is *opened* eagerly (:meth:`LLMGateway.open_text_stream`) so errors before the first
  token can still become a proper HTTP status; errors after it arrive as :class:`StreamFailedError`.
"""

from __future__ import annotations

import random
import time
from collections.abc import AsyncIterator, Awaitable, Callable, Sequence
from dataclasses import dataclass
from typing import Any, TypeVar

import openai
import structlog
from openai import AsyncOpenAI
from pydantic import BaseModel
from tenacity import AsyncRetrying, RetryCallState, retry_if_exception, stop_after_attempt

from ai_service.llm.schemas import strict_json_schema
from ai_service.settings import ModelRole, Settings

log = structlog.get_logger(__name__)

T = TypeVar("T", bound=BaseModel)
R = TypeVar("R")

InputItems = list[dict[str, Any]]


@dataclass(frozen=True)
class LLMUsage:
    deployment: str
    input_tokens: int = 0
    output_tokens: int = 0
    cached_tokens: int = 0
    latency_ms: int = 0


@dataclass(frozen=True)
class TextDelta:
    text: str


@dataclass(frozen=True)
class StreamDone:
    usage: LLMUsage
    incomplete: bool = False
    reason: str | None = None


StreamEvent = TextDelta | StreamDone


class StreamFailedError(Exception):
    """An ``error``/``response.failed`` event arrived mid-stream (HTTP status was already 200)."""

    def __init__(self, code: str | None, message: str = "") -> None:
        super().__init__(message or code or "stream failed")
        self.code = code


def _retry_after_seconds(exc: BaseException) -> float | None:
    if isinstance(exc, openai.APIStatusError):
        for header in ("retry-after-ms", "retry-after"):
            raw = exc.response.headers.get(header)
            if raw:
                try:
                    value = float(raw)
                except ValueError:
                    continue
                return value / 1000 if header == "retry-after-ms" else value
    return None


def _is_retryable(idempotent: bool) -> Callable[[BaseException], bool]:
    def check(exc: BaseException) -> bool:
        if isinstance(exc, openai.RateLimitError | openai.InternalServerError):
            return True
        return idempotent and isinstance(exc, openai.APIConnectionError)

    return check


def _usage_from(response: Any, deployment: str, started: float) -> LLMUsage:
    usage = getattr(response, "usage", None)
    cached = 0
    if usage is not None:
        details = getattr(usage, "input_tokens_details", None)
        cached = int(getattr(details, "cached_tokens", 0) or 0) if details is not None else 0
    return LLMUsage(
        deployment=deployment,
        input_tokens=int(getattr(usage, "input_tokens", 0) or 0),
        output_tokens=int(getattr(usage, "output_tokens", 0) or 0),
        cached_tokens=cached,
        latency_ms=int((time.monotonic() - started) * 1000),
    )


class LLMGateway:
    def __init__(self, client: AsyncOpenAI, settings: Settings, *, retry_wait_scale: float = 1.0) -> None:
        self.client = client
        self.settings = settings
        self._wait_scale = retry_wait_scale

    def deployment(self, role: ModelRole) -> str:
        return self.settings.deployments.for_role(role)

    def _wait(self, state: RetryCallState) -> float:
        exc = state.outcome.exception() if state.outcome else None
        hinted = _retry_after_seconds(exc) if exc else None
        if hinted is not None:
            return float(min(hinted, 30.0)) * self._wait_scale
        base = min(2 ** (state.attempt_number - 1), 16)
        return float(base + random.uniform(0, 1)) * self._wait_scale  # noqa: S311 - jitter, not crypto

    async def _call(
        self, fn: Callable[[], Awaitable[R]], *, idempotent: bool, attempts: int | None = None
    ) -> R:
        retrying = AsyncRetrying(
            stop=stop_after_attempt(attempts or self.settings.llm_max_attempts),
            wait=self._wait,
            retry=retry_if_exception(_is_retryable(idempotent)),
            reraise=True,
            before_sleep=lambda s: log.warning(
                "llm.retry",
                attempt=s.attempt_number,
                error_type=type(s.outcome.exception()).__name__ if s.outcome else None,
            ),
        )
        async for attempt in retrying:
            with attempt:
                return await fn()
        raise AssertionError("unreachable")  # pragma: no cover

    def _sampling(self, role: ModelRole, temperature: float | None) -> dict[str, Any]:
        if role is ModelRole.REASONING:
            return {"reasoning": {"effort": self.settings.reasoning_effort}}
        return {"temperature": temperature} if temperature is not None else {}

    async def open_text_stream(
        self,
        role: ModelRole,
        *,
        instructions: str,
        input_items: InputItems,
        max_output_tokens: int,
        temperature: float | None = None,
    ) -> AsyncIterator[StreamEvent]:
        """Send the request now (retrying 429/5xx) and return an iterator over the stream."""
        deployment = self.deployment(role)
        started = time.monotonic()
        params: dict[str, Any] = {
            "model": deployment,
            "instructions": instructions,
            "input": input_items,
            "max_output_tokens": max_output_tokens,
            "store": False,  # history lives in our SQL tables
            "stream": True,
            **self._sampling(role, temperature),
        }
        stream: Any = await self._call(lambda: self.client.responses.create(**params), idempotent=False)
        return self._iterate(stream, deployment, started)

    async def _iterate(self, stream: Any, deployment: str, started: float) -> AsyncIterator[StreamEvent]:
        try:
            async for event in stream:
                kind = getattr(event, "type", "")
                if kind == "response.output_text.delta":
                    yield TextDelta(str(event.delta))
                elif kind == "response.completed":
                    yield StreamDone(_usage_from(event.response, deployment, started))
                    return
                elif kind == "response.incomplete":
                    details = getattr(event.response, "incomplete_details", None)
                    reason = getattr(details, "reason", None)
                    yield StreamDone(
                        _usage_from(event.response, deployment, started), incomplete=True, reason=reason
                    )
                    return
                elif kind == "response.failed":
                    error = getattr(event.response, "error", None)
                    raise StreamFailedError(getattr(error, "code", None), getattr(error, "message", ""))
                elif kind == "error":
                    raise StreamFailedError(getattr(event, "code", None), getattr(event, "message", ""))
            raise StreamFailedError("stream_ended", "stream ended without a completion event")
        finally:
            close = getattr(stream, "close", None)
            if close is not None:
                await close()

    async def parse(
        self,
        role: ModelRole,
        schema: type[T],
        *,
        instructions: str,
        input_items: InputItems,
        max_output_tokens: int = 400,
        temperature: float | None = 0.0,
    ) -> tuple[T, LLMUsage]:
        """Structured output: ``text.format`` = strict JSON schema from a Pydantic model."""
        deployment = self.deployment(role)
        started = time.monotonic()
        params: dict[str, Any] = {
            "model": deployment,
            "instructions": instructions,
            "input": input_items,
            "max_output_tokens": max_output_tokens,
            "store": False,
            "text": {
                "format": {
                    "type": "json_schema",
                    "name": schema.__name__,
                    "schema": strict_json_schema(schema),
                    "strict": True,
                }
            },
            **self._sampling(role, temperature),
        }
        response: Any = await self._call(lambda: self.client.responses.create(**params), idempotent=True)
        return schema.model_validate_json(str(response.output_text)), _usage_from(
            response, deployment, started
        )

    async def embed(
        self, texts: Sequence[str], *, batch_size: int = 64
    ) -> tuple[list[list[float]], LLMUsage]:
        deployment = self.deployment(ModelRole.EMBED)
        started = time.monotonic()
        vectors: list[list[float]] = []
        tokens = 0
        for start in range(0, len(texts), batch_size):
            batch = list(texts[start : start + batch_size])
            response = await self._call(
                lambda b=batch: self.client.embeddings.create(  # type: ignore[misc]
                    model=deployment, input=b, dimensions=self.settings.embed_dimensions
                ),
                idempotent=True,
            )
            vectors.extend(item.embedding for item in sorted(response.data, key=lambda d: d.index))
            tokens += response.usage.prompt_tokens
        usage = LLMUsage(
            deployment=deployment,
            input_tokens=tokens,
            latency_ms=int((time.monotonic() - started) * 1000),
        )
        return vectors, usage

    async def ping(self) -> None:
        """Readiness: list models (cheap, authenticates with the same identity)."""
        await self.client.models.list()
