"""Errors in the backend shape: ``{ "error": { "code", "date", "messages": [...], "source": "Ai" } }``.

The web and mobile apps already handle this shape for the .NET modules, so AI errors look the same.
Message keys are localized by ``Accept-Language`` (Arabic or English).
"""

from __future__ import annotations

from datetime import UTC, datetime
from typing import Any, cast

import openai
import structlog
from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from starlette.exceptions import HTTPException as StarletteHTTPException

log = structlog.get_logger(__name__)

Locale = str  # "ar" | "en"

MESSAGES: dict[str, dict[str, str]] = {
    "General:Errors:AiBusy": {
        "en": "The coach is busy right now. Please try again in a moment.",
        "ar": "المدرّب مشغول الآن. يُرجى المحاولة بعد قليل.",
    },
    "General:Errors:ContentBlocked": {
        "en": "This request can't be answered because it was blocked by the content filter.",
        "ar": "تعذّرت الإجابة عن هذا الطلب لأن مرشّح المحتوى حجبه.",
    },
    "General:Errors:AiTimeout": {
        "en": "The coach took too long to answer. Please try again.",
        "ar": "استغرق المدرّب وقتًا طويلًا في الإجابة. يُرجى المحاولة مرة أخرى.",
    },
    "General:Errors:AiUnavailable": {
        "en": "The coach is unavailable right now. Please try again later.",
        "ar": "المدرّب غير متاح حاليًا. يُرجى المحاولة لاحقًا.",
    },
    "General:Errors:ContextTooLong": {
        "en": "The conversation is too long. Please start a new one.",
        "ar": "المحادثة طويلة جدًا. يُرجى بدء محادثة جديدة.",
    },
    "General:Errors:Unexpected": {
        "en": "Something went wrong. Please try again later.",
        "ar": "حدث خطأ ما. يُرجى المحاولة لاحقًا.",
    },
    "General:Errors:Unauthorized": {
        "en": "Please sign in again.",
        "ar": "يُرجى تسجيل الدخول مرة أخرى.",
    },
    "General:Errors:Forbidden": {
        "en": "You don't have permission to do this.",
        "ar": "ليست لديك صلاحية للقيام بذلك.",
    },
    "General:Errors:NotFound": {
        "en": "The requested item was not found.",
        "ar": "العنصر المطلوب غير موجود.",
    },
    "General:Errors:Validation": {
        "en": "Some of the information sent is not valid.",
        "ar": "بعض البيانات المرسلة غير صالحة.",
    },
    "General:Errors:TooManyRequests": {
        "en": "You're asking faster than the coach can answer. Please wait a minute.",
        "ar": "أسئلتك أسرع مما يستطيع المدرّب الإجابة عنه. يُرجى الانتظار دقيقة.",
    },
    "Coach:Errors:MoveMismatch": {
        "en": "A key moment does not match the game record.",
        "ar": "إحدى اللحظات الحاسمة لا تطابق سجل المباراة.",
    },
}


def resolve_locale(accept_language: str | None) -> Locale:
    """Pick ``ar`` or ``en`` from an Accept-Language header (default ``en``)."""
    if not accept_language:
        return "en"
    best: tuple[float, str] = (-1.0, "en")
    for part in accept_language.split(","):
        pieces = part.strip().split(";")
        tag = pieces[0].strip().lower()
        q = 1.0
        for raw in pieces[1:]:
            param = raw.strip()
            if param.startswith("q="):
                try:
                    q = float(param[2:])
                except ValueError:
                    q = 0.0
        lang = tag.split("-")[0]
        if lang in ("ar", "en") and q > best[0]:
            best = (q, lang)
    return best[1]


def localize(key: str, locale: Locale) -> str:
    entry = MESSAGES.get(key)
    if entry is None:
        return key
    return entry.get(locale) or entry["en"]


class AppError(Exception):
    """An error with a localized message key and an HTTP status."""

    def __init__(
        self,
        code: str,
        status_code: int,
        *,
        details: list[str] | None = None,
        headers: dict[str, str] | None = None,
    ) -> None:
        super().__init__(code)
        self.code = code
        self.status_code = status_code
        self.details = details or []
        self.headers = headers or {}


def error_body(code: str, messages: list[str]) -> dict[str, Any]:
    return {
        "error": {
            "code": code,
            "date": datetime.now(UTC).isoformat(),
            "messages": messages,
            "source": "Ai",
        }
    }


def _openai_error_code(exc: openai.APIError) -> str | None:
    code = getattr(exc, "code", None)
    if isinstance(code, str):
        return code
    body = exc.body
    if isinstance(body, dict):
        inner = body.get("error", body)
        if isinstance(inner, dict) and isinstance(inner.get("code"), str):
            return str(inner["code"])
    return None


def map_openai_error(exc: openai.OpenAIError) -> AppError:
    """Translate an OpenAI SDK exception (after our retries are exhausted) into an AppError."""
    if isinstance(exc, openai.RateLimitError):
        return AppError("General:Errors:AiBusy", 503, headers={"Retry-After": "10"})
    if isinstance(exc, openai.APITimeoutError):  # subclass of APIConnectionError: check first
        return AppError("General:Errors:AiTimeout", 504)
    if isinstance(exc, openai.APIConnectionError):
        return AppError("General:Errors:AiTimeout", 504)
    if isinstance(exc, openai.BadRequestError):
        code = _openai_error_code(exc)
        if code == "content_filter" or "content_filter" in str(exc.message):
            return AppError("General:Errors:ContentBlocked", 400)
        if code == "context_length_exceeded":
            return AppError("General:Errors:ContextTooLong", 400)
        return AppError("General:Errors:Unexpected", 500)
    if isinstance(exc, openai.AuthenticationError | openai.PermissionDeniedError):
        log.error("ai.identity_misconfigured", error_type=type(exc).__name__)
        return AppError("General:Errors:Unexpected", 500)
    if isinstance(exc, openai.InternalServerError):
        return AppError("General:Errors:AiUnavailable", 502)
    if isinstance(exc, openai.APIStatusError):
        return AppError("General:Errors:AiUnavailable", 502)
    return AppError("General:Errors:Unexpected", 500)


def is_content_filter(exc: BaseException) -> bool:
    return isinstance(exc, openai.BadRequestError) and (
        _openai_error_code(exc) == "content_filter" or "content_filter" in str(exc.message)
    )


def to_app_error(exc: BaseException) -> AppError:
    if isinstance(exc, AppError):
        return exc
    if isinstance(exc, openai.OpenAIError):
        return map_openai_error(exc)
    return AppError("General:Errors:Unexpected", 500)


def render(exc: AppError, locale: Locale) -> JSONResponse:
    messages = [localize(exc.code, locale), *exc.details]
    return JSONResponse(error_body(exc.code, messages), status_code=exc.status_code, headers=exc.headers)


_STATUS_CODES = {
    401: "General:Errors:Unauthorized",
    403: "General:Errors:Forbidden",
    404: "General:Errors:NotFound",
    405: "General:Errors:NotFound",
    429: "General:Errors:TooManyRequests",
}


def install_error_handlers(app: FastAPI) -> None:
    def _locale(request: Request) -> Locale:
        return resolve_locale(request.headers.get("accept-language"))

    async def app_error(request: Request, exc: Exception) -> JSONResponse:
        exc = cast(AppError, exc)
        return render(exc, _locale(request))

    async def openai_error(request: Request, exc: Exception) -> JSONResponse:
        exc = cast(openai.OpenAIError, exc)
        mapped = map_openai_error(exc)
        log.warning("ai.upstream_error", error_type=type(exc).__name__, code=mapped.code)
        return render(mapped, _locale(request))

    async def validation_error(request: Request, exc: Exception) -> JSONResponse:
        exc = cast(RequestValidationError, exc)
        details = [
            f"{'.'.join(str(p) for p in err.get('loc', ()) if p != 'body')}: {err.get('msg', '')}"
            for err in exc.errors()
        ]
        return render(AppError("General:Errors:Validation", 400, details=details), _locale(request))

    async def http_error(request: Request, exc: Exception) -> JSONResponse:
        exc = cast(StarletteHTTPException, exc)
        code = _STATUS_CODES.get(exc.status_code, "General:Errors:Unexpected")
        return render(AppError(code, exc.status_code, headers=dict(exc.headers or {})), _locale(request))

    async def unhandled(request: Request, exc: Exception) -> JSONResponse:
        log.exception("ai.unhandled_error", error_type=type(exc).__name__)
        return render(AppError("General:Errors:Unexpected", 500), _locale(request))

    app.add_exception_handler(AppError, app_error)
    app.add_exception_handler(openai.OpenAIError, openai_error)
    app.add_exception_handler(RequestValidationError, validation_error)
    app.add_exception_handler(StarletteHTTPException, http_error)
    app.add_exception_handler(Exception, unhandled)
