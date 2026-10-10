"""Index schema, migrations, rate limiter, client construction."""

from pathlib import Path

import httpx
import pytest
import respx
from alembic import command
from alembic.config import Config
from sqlalchemy import create_engine, inspect

from ai_service.llm.client import build_client
from ai_service.rag.index.schema import build_index
from ai_service.ratelimit import SlidingWindowLimiter
from ai_service.settings import Settings
from tests.helpers import OPENAI_BASE

ROOT = Path(__file__).resolve().parents[2]


def test_index_schema(settings: Settings) -> None:
    settings.search_index_name = "qala-knowledge"
    index = build_index(settings)
    fields = {f.name: f for f in index.fields}
    for name in (
        "id",
        "tenant_id",
        "acl_groups",
        "document_id",
        "content",
        "content_search",
        "content_vector",
        "heading_path",
        "page",
        "effective_date",
        "source_url",
        "language",
    ):
        assert name in fields
    assert fields["id"].key
    assert fields["tenant_id"].filterable and fields["acl_groups"].filterable
    assert fields["content_ar"].analyzer_name == "ar.microsoft"
    assert fields["content_en"].analyzer_name == "en.microsoft"
    assert fields["content_vector"].vector_search_dimensions == settings.embed_dimensions
    assert index.semantic_search is not None and index.semantic_search.configurations
    semantic = index.semantic_search.configurations[0].prioritized_fields
    assert semantic.title_field is not None and semantic.title_field.field_name == "title"
    assert semantic.keywords_fields and semantic.keywords_fields[0].field_name == "heading_path"


def test_index_name_is_configuration(settings: Settings) -> None:
    with pytest.raises(RuntimeError):
        build_index(settings)


def test_alembic_upgrade_creates_tables(tmp_path: Path) -> None:
    db = tmp_path / "migrated.db"
    cfg = Config(str(ROOT / "alembic.ini"))
    cfg.set_main_option("sqlalchemy.url", f"sqlite+aiosqlite:///{db}")
    cfg.attributes["configure_logger"] = False
    command.upgrade(cfg, "head")
    tables = set(inspect(create_engine(f"sqlite:///{db}")).get_table_names())
    assert {"conversations", "messages", "ingestion_jobs", "usage"} <= tables
    command.downgrade(cfg, "base")


def test_sliding_window_limiter() -> None:
    now = [0.0]
    limiter = SlidingWindowLimiter(2, 60.0, clock=lambda: now[0])
    assert limiter.hit("u") is None
    assert limiter.hit("u") is None
    assert limiter.hit("u") == pytest.approx(60.0)
    assert limiter.hit("other") is None
    now[0] = 61.0
    assert limiter.hit("u") is None


def test_client_uses_v1_endpoint_and_rejects_keys_outside_local(settings: Settings) -> None:
    client = build_client(settings)
    assert str(client.base_url) == "https://unit-test-resource.openai.azure.com/openai/v1/"
    assert client.max_retries == 0
    settings.environment = "production"
    with pytest.raises(RuntimeError, match="local development only"):
        build_client(settings)


async def test_client_sends_a_fresh_entra_token(settings: Settings, mock_http: respx.MockRouter) -> None:
    settings.azure_openai_api_key = None
    tokens = iter(["entra-token-1", "entra-token-2"])

    async def provider() -> str:
        return next(tokens)

    client = build_client(settings, token_provider=provider)
    route = mock_http.get(f"{OPENAI_BASE}models").mock(
        return_value=httpx.Response(200, json={"object": "list", "data": []})
    )
    await client.models.list()
    await client.models.list()
    assert [c.request.headers["authorization"] for c in route.calls] == [
        "Bearer entra-token-1",
        "Bearer entra-token-2",
    ]
