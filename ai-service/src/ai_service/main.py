"""FastAPI app factory."""

from __future__ import annotations

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI

from ai_service.api.errors import install_error_handlers
from ai_service.api.routes import admin, chat, coach, health, ingest, search
from ai_service.container import Container, build_container
from ai_service.observability import configure_logging
from ai_service.settings import Settings, get_settings


def create_app(settings: Settings | None = None, container: Container | None = None) -> FastAPI:
    settings = settings or (container.settings if container else get_settings())

    @asynccontextmanager
    async def lifespan(app: FastAPI) -> AsyncIterator[None]:
        if container is not None:
            app.state.container = container
            yield
            return
        configure_logging(settings.log_level, json=settings.environment != "local")
        built = await build_container(settings)
        app.state.container = built
        try:
            yield
        finally:
            await built.aclose()

    app = FastAPI(
        title="Qal'a AI service",
        version="0.1.0",
        lifespan=lifespan,
        docs_url="/ai-api/docs" if settings.environment in ("local", "dev") else None,
        openapi_url="/ai-api/openapi.json" if settings.environment in ("local", "dev") else None,
    )
    install_error_handlers(app)
    for module in (health, chat, coach, search, ingest, admin):
        app.include_router(module.router)
    return app


def app() -> FastAPI:  # uvicorn --factory ai_service.main:app
    return create_app()
