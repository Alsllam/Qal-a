"""Service configuration.

Everything that differs between environments (endpoints, deployment names, limits, prices)
is read from environment variables. Nothing here holds a real endpoint, key or deployment name.
"""

from __future__ import annotations

from enum import StrEnum
from functools import lru_cache
from typing import Literal

from pydantic import BaseModel, Field, SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict


class ModelRole(StrEnum):
    """What a model call is for. Each role maps to an Azure deployment in configuration."""

    CHAT = "chat"
    FAST = "fast"
    REASONING = "reasoning"
    EMBED = "embed"


class Deployments(BaseModel):
    """Azure OpenAI deployment name per role (env: DEPLOYMENTS__CHAT, DEPLOYMENTS__FAST, ...)."""

    chat: str = ""
    fast: str = ""
    reasoning: str = ""
    embed: str = ""

    def for_role(self, role: ModelRole) -> str:
        name: str = getattr(self, role.value)
        if not name:
            msg = f"No deployment configured for model role '{role.value}' (DEPLOYMENTS__{role.name})"
            raise RuntimeError(msg)
        return name


class Price(BaseModel):
    """USD per 1M tokens for one deployment."""

    input: float = 0.0
    cached_input: float = 0.0
    output: float = 0.0


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        env_nested_delimiter="__",
        extra="ignore",
    )

    environment: Literal["local", "test", "dev", "staging", "production"] = "local"
    product: str = "qala"
    default_tenant_id: str = "qala"

    # --- Azure OpenAI ---
    azure_openai_endpoint: str = ""
    azure_openai_api_key: SecretStr | None = None  # local development only
    azure_openai_token_scope: str = "https://ai.azure.com/.default"  # noqa: S105 - OAuth scope, not a secret
    deployments: Deployments = Field(default_factory=Deployments)
    embed_dimensions: int = 1536
    llm_timeout_s: float = 60.0
    llm_max_attempts: int = 4
    chat_temperature: float = 0.3
    chat_max_output_tokens: int = 700
    coach_max_output_tokens: int = 350
    reasoning_effort: Literal["low", "medium", "high"] = "medium"

    # --- Azure AI Search ---
    # "memory" = in-process BM25 over seeded Markdown (local development / offline evals only).
    search_backend: Literal["azure", "memory"] = "azure"
    memory_seed_paths: list[str] = Field(default_factory=list)
    search_endpoint: str = ""
    search_api_key: SecretStr | None = None  # local development only
    search_index_name: str = ""
    search_semantic_config: str = "default"
    search_top: int = 20
    search_vector_k: int = 50
    reranker_threshold: float = 1.5
    max_chunks_per_document: int = 3

    # --- Prompts and context ---
    answer_prompt_version: str = "v1"
    coach_prompt_version: str = "v1"
    rewrite_prompt_version: str = "v1"
    context_budget_tokens: int = 6000
    coach_context_budget_tokens: int = 2500
    history_turns: int = 6
    tokenizer_mode: Literal["auto", "tiktoken", "estimate"] = "auto"

    # --- Chunking ---
    chunk_min_tokens: int = 400
    chunk_max_tokens: int = 800
    chunk_overlap_ratio: float = 0.12

    # --- Auth (OpenIddict) ---
    jwt_jwks_url: str = ""
    jwt_issuer: str = ""
    jwt_audience: str = "ai-api"
    jwt_algorithms: list[str] = Field(default_factory=lambda: ["RS256"])
    jwt_leeway_s: int = 30
    jwks_cache_ttl_s: int = 3600
    tenant_claim: str = "tenant_id"
    roles_claim: str = "role"
    permissions_claim: str = "permission"
    public_acl_group: str = "public"
    ingest_permission: str = "Permissions.Content.ManageLessons"
    admin_permission: str = "Permissions.Dashboard.ViewBalance"

    # --- Limits ---
    rate_limit_per_minute: int = 20
    rate_limit_window_s: float = 60.0
    retention_days: int = 90

    # --- Prompt Shields (Azure AI Content Safety), optional ---
    content_safety_endpoint: str = ""
    content_safety_api_key: SecretStr | None = None  # local development only
    prompt_shields_enabled: bool = False

    # --- Storage ---
    database_url: str = "sqlite+aiosqlite:///./ai_service.db"

    # --- Cost (USD per 1M tokens, keyed by deployment name), e.g. PRICES='{"my-chat":{"input":2}}'
    prices: dict[str, Price] = Field(default_factory=dict)

    log_level: str = "INFO"

    @property
    def azure_openai_base_url(self) -> str:
        return f"{self.azure_openai_endpoint.rstrip('/')}/openai/v1/"


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
