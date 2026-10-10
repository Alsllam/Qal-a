"""Wires the service's dependencies. Tests build a Container with fakes instead."""

from __future__ import annotations

import asyncio
from collections.abc import Awaitable, Callable
from dataclasses import dataclass, field
from pathlib import Path

import httpx
from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncEngine, AsyncSession, async_sessionmaker

from ai_service.api.errors import AppError
from ai_service.auth import JwksCache, TokenValidator
from ai_service.coach.review import CoachService
from ai_service.conversations.repository import ConversationRepository
from ai_service.db import build_engine, build_sessionmaker
from ai_service.ingest_service import IngestService
from ai_service.llm.client import build_client, build_token_provider
from ai_service.llm.gateway import LLMGateway
from ai_service.rag.answer import AnswerService
from ai_service.rag.index.memory import InMemoryIndex
from ai_service.rag.index.writer import AzureSearchIndexWriter
from ai_service.rag.ingestion.chunker import ChunkerConfig, MarkdownChunker
from ai_service.rag.ingestion.pipeline import IndexWriter, IngestionPipeline
from ai_service.rag.retrieval.search import AzureHybridRetriever, Retriever
from ai_service.rag.types import SourceDocument
from ai_service.ratelimit import SlidingWindowLimiter
from ai_service.safety.prompt_shields import PromptShields
from ai_service.settings import Settings
from ai_service.tokens import TokenCounter
from ai_service.usage.meter import UsageMeter


@dataclass
class Container:
    settings: Settings
    engine: AsyncEngine
    sessions: async_sessionmaker[AsyncSession]
    counter: TokenCounter
    validator: TokenValidator
    limiter: SlidingWindowLimiter
    conversations: ConversationRepository
    meter: UsageMeter
    retriever: Retriever
    index_writer: IndexWriter
    gateway: LLMGateway | None
    ingest: IngestService
    shields: PromptShields | None = None
    closers: list[Callable[[], Awaitable[None]]] = field(default_factory=list)

    def _require_gateway(self) -> LLMGateway:
        if self.gateway is None:
            raise AppError("General:Errors:AiUnavailable", 503)
        return self.gateway

    @property
    def answers(self) -> AnswerService:
        return AnswerService(
            settings=self.settings,
            gateway=self._require_gateway(),
            retriever=self.retriever,
            conversations=self.conversations,
            meter=self.meter,
            counter=self.counter,
            shields=self.shields,
        )

    @property
    def coach(self) -> CoachService:
        return CoachService(
            settings=self.settings,
            gateway=self._require_gateway(),
            retriever=self.retriever,
            meter=self.meter,
            counter=self.counter,
        )

    async def check_ready(self) -> dict[str, str]:
        checks: dict[str, str] = {}
        try:
            async with self.engine.connect() as conn:
                await conn.execute(text("SELECT 1"))
            checks["database"] = "ok"
        except Exception as exc:
            checks["database"] = f"fail: {type(exc).__name__}"
        try:
            await self.retriever.ping()
            checks["search"] = "ok"
        except Exception as exc:
            checks["search"] = f"fail: {type(exc).__name__}"
        if self.gateway is None:
            checks["openai"] = "fail: not configured"
        else:
            try:
                await self.gateway.ping()
                checks["openai"] = "ok"
            except Exception as exc:
                checks["openai"] = f"fail: {type(exc).__name__}"
        return checks

    async def aclose(self) -> None:
        for close in reversed(self.closers):
            await close()


def build_pipeline(
    settings: Settings, counter: TokenCounter, *, writer: IndexWriter | None, gateway: LLMGateway | None
) -> IngestionPipeline:
    chunker = MarkdownChunker(
        counter,
        ChunkerConfig(settings.chunk_min_tokens, settings.chunk_max_tokens, settings.chunk_overlap_ratio),
    )
    return IngestionPipeline(chunker, product=settings.product, embedder=gateway, writer=writer)


async def seed_memory_index(
    index: InMemoryIndex, settings: Settings, counter: TokenCounter, paths: list[str]
) -> None:
    pipeline = build_pipeline(settings, counter, writer=index, gateway=None)
    for raw in paths:
        path = Path(raw)
        markdown = await asyncio.to_thread(path.read_text, encoding="utf-8")
        title = next(
            (ln.lstrip("# ").strip() for ln in markdown.splitlines() if ln.startswith("# ")), path.stem
        )
        await pipeline.run(
            SourceDocument(
                tenant_id=settings.default_tenant_id,
                document_id=path.stem,
                title=title,
                markdown=markdown,
                acl_groups=(settings.public_acl_group,),
                doc_type="rules" if path.stem == "rules" else "lesson",
                source_url=f"docs/{path.name}",
            )
        )


async def build_container(settings: Settings) -> Container:
    http = httpx.AsyncClient()
    engine = build_engine(settings.database_url)
    sessions = build_sessionmaker(engine)
    counter = TokenCounter(settings.tokenizer_mode)
    closers: list[Callable[[], Awaitable[None]]] = [http.aclose, engine.dispose]

    gateway: LLMGateway | None = None
    if settings.azure_openai_endpoint:
        client = build_client(settings)
        gateway = LLMGateway(client, settings)
        closers.append(client.close)

    retriever: Retriever
    writer: IndexWriter
    if settings.search_backend == "memory":
        if settings.environment == "production":
            raise RuntimeError("SEARCH_BACKEND=memory is for local development only")
        memory = InMemoryIndex()
        await seed_memory_index(memory, settings, counter, settings.memory_seed_paths)
        retriever, writer = memory, memory
    else:
        from azure.core.credentials import AzureKeyCredential
        from azure.core.credentials_async import AsyncTokenCredential
        from azure.identity.aio import DefaultAzureCredential
        from azure.search.documents.aio import SearchClient

        if not (settings.search_endpoint and settings.search_index_name):
            raise RuntimeError("SEARCH_ENDPOINT and SEARCH_INDEX_NAME are required")
        credential: AzureKeyCredential | AsyncTokenCredential
        if settings.search_api_key is not None:
            credential = AzureKeyCredential(settings.search_api_key.get_secret_value())
        else:
            aad = DefaultAzureCredential()
            credential = aad
            closers.append(aad.close)
        search_client = SearchClient(settings.search_endpoint, settings.search_index_name, credential)
        closers.append(search_client.close)
        if gateway is None:
            raise RuntimeError("Azure hybrid search needs AZURE_OPENAI_ENDPOINT for query embeddings")
        retriever = AzureHybridRetriever(search_client, gateway, settings)
        writer = AzureSearchIndexWriter(search_client)

    shields: PromptShields | None = None
    if settings.prompt_shields_enabled and settings.content_safety_endpoint:
        key = settings.content_safety_api_key
        shields = PromptShields(
            http,
            settings.content_safety_endpoint,
            api_key=key.get_secret_value() if key else None,
            token_provider=None
            if key
            else build_token_provider("https://cognitiveservices.azure.com/.default"),
        )

    pipeline = build_pipeline(settings, counter, writer=writer, gateway=gateway)
    return Container(
        settings=settings,
        engine=engine,
        sessions=sessions,
        counter=counter,
        validator=TokenValidator(
            settings, JwksCache(http, settings.jwt_jwks_url, ttl_s=settings.jwks_cache_ttl_s)
        ),
        limiter=SlidingWindowLimiter(settings.rate_limit_per_minute, settings.rate_limit_window_s),
        conversations=ConversationRepository(sessions),
        meter=UsageMeter(sessions, settings.prices),
        retriever=retriever,
        index_writer=writer,
        gateway=gateway,
        ingest=IngestService(sessions, pipeline),
        shields=shields,
        closers=closers,
    )
