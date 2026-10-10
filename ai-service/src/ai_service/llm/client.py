"""Build the OpenAI SDK client for the Azure OpenAI v1 endpoint.

Deployed environments authenticate with Microsoft Entra ID (managed identity with the
*Cognitive Services OpenAI User* role). The SDK calls the async token provider before each request,
and ``azure-identity`` caches and refreshes the token before it expires. An API key is accepted only
for local development (``AZURE_OPENAI_API_KEY`` in a git-ignored ``.env``).
"""

from __future__ import annotations

from collections.abc import Awaitable, Callable

from openai import AsyncOpenAI

from ai_service.settings import Settings

TokenProvider = Callable[[], Awaitable[str]]


def build_token_provider(scope: str) -> TokenProvider:
    from azure.identity.aio import DefaultAzureCredential, get_bearer_token_provider

    return get_bearer_token_provider(DefaultAzureCredential(), scope)


def build_client(settings: Settings, token_provider: TokenProvider | None = None) -> AsyncOpenAI:
    if not settings.azure_openai_endpoint:
        raise RuntimeError("AZURE_OPENAI_ENDPOINT is not configured")
    base_url = settings.azure_openai_base_url
    if settings.azure_openai_api_key is not None:
        if settings.environment not in ("local", "test"):
            raise RuntimeError("API keys are for local development only; use Entra ID")
        return AsyncOpenAI(
            base_url=base_url,
            api_key=settings.azure_openai_api_key.get_secret_value(),
            max_retries=0,  # retries are handled by our tenacity policy (llm/gateway.py)
            timeout=settings.llm_timeout_s,
        )
    provider = token_provider or build_token_provider(settings.azure_openai_token_scope)
    return AsyncOpenAI(
        base_url=base_url,
        api_key=provider,
        max_retries=0,
        timeout=settings.llm_timeout_s,
    )
