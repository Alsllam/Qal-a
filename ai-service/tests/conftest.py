from __future__ import annotations

from collections.abc import AsyncIterator, Iterator
from pathlib import Path
from typing import Any

import httpx
import pytest
import respx
from fastapi import FastAPI
from openai import AsyncOpenAI

from ai_service.auth import JwksCache, TokenValidator
from ai_service.container import Container, build_pipeline, seed_memory_index
from ai_service.conversations.repository import ConversationRepository
from ai_service.db import Base, build_engine, build_sessionmaker
from ai_service.ingest_service import IngestService
from ai_service.llm.gateway import LLMGateway
from ai_service.main import create_app
from ai_service.rag.index.memory import InMemoryIndex
from ai_service.ratelimit import SlidingWindowLimiter
from ai_service.settings import Deployments, Settings
from ai_service.tokens import TokenCounter
from ai_service.usage.meter import UsageMeter
from tests.helpers import AUTH_ISSUER, JWKS_URL, OPENAI_BASE, OPENAI_ENDPOINT, KeyPair

REPO_ROOT = Path(__file__).resolve().parents[2]
RULES_MD = REPO_ROOT / "docs" / "rules.md"


@pytest.fixture(scope="session")
def keys() -> KeyPair:
    return KeyPair()


@pytest.fixture
def settings(tmp_path: Path) -> Settings:
    return Settings(
        _env_file=None,
        environment="test",
        azure_openai_endpoint=OPENAI_ENDPOINT,
        azure_openai_api_key="unit-test-key",
        deployments=Deployments(chat="chat-deployment", fast="fast-deployment", embed="embed-deployment"),
        jwt_jwks_url=JWKS_URL,
        jwt_issuer=AUTH_ISSUER,
        database_url=f"sqlite+aiosqlite:///{tmp_path / 'test.db'}",
        tokenizer_mode="estimate",
        search_backend="memory",
        rate_limit_per_minute=5,
    )


@pytest.fixture
def mock_http(keys: KeyPair) -> Iterator[respx.MockRouter]:
    with respx.mock(assert_all_called=False, assert_all_mocked=True) as router:
        router.get(JWKS_URL).mock(return_value=httpx.Response(200, json=keys.jwks()))
        yield router


@pytest.fixture
async def container(settings: Settings, mock_http: respx.MockRouter) -> AsyncIterator[Container]:
    engine = build_engine(settings.database_url)
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    sessions = build_sessionmaker(engine)
    counter = TokenCounter("estimate")
    http = httpx.AsyncClient()
    client = AsyncOpenAI(base_url=OPENAI_BASE, api_key="unit-test-key", max_retries=0)
    gateway = LLMGateway(client, settings, retry_wait_scale=0.0)
    index = InMemoryIndex()
    await seed_memory_index(index, settings, counter, [str(RULES_MD)])
    c = Container(
        settings=settings,
        engine=engine,
        sessions=sessions,
        counter=counter,
        validator=TokenValidator(settings, JwksCache(http, settings.jwt_jwks_url)),
        limiter=SlidingWindowLimiter(settings.rate_limit_per_minute, settings.rate_limit_window_s),
        conversations=ConversationRepository(sessions),
        meter=UsageMeter(sessions, settings.prices),
        retriever=index,
        index_writer=index,
        gateway=gateway,
        ingest=IngestService(sessions, build_pipeline(settings, counter, writer=index, gateway=None)),
        closers=[http.aclose, client.close, engine.dispose],
    )
    yield c
    await c.aclose()


@pytest.fixture
def app(container: Container) -> FastAPI:
    application = create_app(container=container)
    application.state.container = container
    return application


@pytest.fixture
async def api(app: FastAPI) -> AsyncIterator[httpx.AsyncClient]:
    async with httpx.AsyncClient(transport=httpx.ASGITransport(app=app), base_url="http://ai-service") as c:
        yield c


@pytest.fixture
def auth(keys: KeyPair) -> dict[str, str]:
    return {"Authorization": f"Bearer {keys.token()}"}


def bearer(keys: KeyPair, **claims: Any) -> dict[str, str]:
    return {"Authorization": f"Bearer {keys.token(**claims)}"}
