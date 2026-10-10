import httpx
import openai
import pytest

from ai_service.api.errors import error_body, localize, map_openai_error, resolve_locale

REQ = httpx.Request("POST", "https://x.openai.azure.com/openai/v1/responses")


def status_error(cls: type[openai.APIStatusError], status: int, body: object = None) -> openai.APIStatusError:
    return cls("upstream error", response=httpx.Response(status, request=REQ), body=body)


@pytest.mark.parametrize(
    ("exc", "code", "status"),
    [
        (status_error(openai.RateLimitError, 429), "General:Errors:AiBusy", 503),
        (openai.APITimeoutError(request=REQ), "General:Errors:AiTimeout", 504),
        (openai.APIConnectionError(request=REQ), "General:Errors:AiTimeout", 504),
        (
            status_error(openai.BadRequestError, 400, {"code": "content_filter", "message": "filtered"}),
            "General:Errors:ContentBlocked",
            400,
        ),
        (
            status_error(openai.BadRequestError, 400, {"code": "context_length_exceeded"}),
            "General:Errors:ContextTooLong",
            400,
        ),
        (
            status_error(openai.BadRequestError, 400, {"code": "invalid_value"}),
            "General:Errors:Unexpected",
            500,
        ),
        (status_error(openai.AuthenticationError, 401), "General:Errors:Unexpected", 500),
        (status_error(openai.PermissionDeniedError, 403), "General:Errors:Unexpected", 500),
        (status_error(openai.InternalServerError, 500), "General:Errors:AiUnavailable", 502),
        (status_error(openai.NotFoundError, 404), "General:Errors:AiUnavailable", 502),
    ],
)
def test_openai_errors_map_to_backend_codes(exc: openai.OpenAIError, code: str, status: int) -> None:
    mapped = map_openai_error(exc)
    assert (mapped.code, mapped.status_code) == (code, status)


def test_rate_limit_sets_retry_after() -> None:
    assert map_openai_error(status_error(openai.RateLimitError, 429)).headers["Retry-After"]


def test_error_body_shape() -> None:
    body = error_body("General:Errors:AiBusy", ["busy"])
    assert set(body["error"]) == {"code", "date", "messages", "source"}
    assert body["error"]["source"] == "Ai"


def test_localization() -> None:
    assert resolve_locale("ar-SA,ar;q=0.9,en;q=0.8") == "ar"
    assert resolve_locale("fr-FR, en;q=0.5") == "en"
    assert resolve_locale(None) == "en"
    assert localize("General:Errors:ContentBlocked", "ar").startswith("تعذّرت")
    assert "busy" in localize("General:Errors:AiBusy", "en")
