"""Query rewrite with the ``fast`` model and structured output."""

from __future__ import annotations

import json
import re

import openai
import structlog

from ai_service.api.errors import is_content_filter
from ai_service.llm.gateway import LLMGateway, LLMUsage
from ai_service.llm.prompts import load_prompt
from ai_service.llm.schemas import QueryRewrite
from ai_service.rag.glossary import expand_arabic
from ai_service.rag.ingestion.normalize import detect_language
from ai_service.settings import ModelRole

log = structlog.get_logger(__name__)

_SMALL_TALK = re.compile(
    r"^\s*(hi|hello|hey|thanks|thank you|ok|okay|مرحبا|مرحبًا|أهلا|اهلا|السلام عليكم|شكرا|شكرًا)[\s!.؟?]*$",
    re.I,
)


def fallback_rewrite(message: str, locale: str | None = None) -> QueryRewrite:
    """Deterministic rewrite used when the model is unavailable (and for the first turn heuristics)."""
    language = detect_language(message)
    if language not in ("ar", "en"):
        language = locale or "en"
    return QueryRewrite(
        standalone_query=message.strip(),
        search_query_en=expand_arabic(message) if language == "ar" else message.strip(),
        language="ar" if language == "ar" else "en",
        needs_retrieval=not _SMALL_TALK.match(message),
        topic=None,
    )


async def rewrite_query(
    gateway: LLMGateway,
    message: str,
    history: list[tuple[str, str]],
    *,
    version: str,
) -> tuple[QueryRewrite, LLMUsage | None]:
    """Make the question standalone and add English keywords.

    An English first turn needs neither, so it skips the model (cost); Arabic questions always go
    through it because the knowledge base is written in English.
    """
    quick = fallback_rewrite(message)
    if not quick.needs_retrieval or (not history and quick.language == "en"):
        return quick, None
    turns = [{"role": role, "text": text[:1500]} for role, text in history]
    payload = json.dumps({"conversation": turns, "message": message}, ensure_ascii=False)
    try:
        result, usage = await gateway.parse(
            ModelRole.FAST,
            QueryRewrite,
            instructions=load_prompt("query_rewrite", version),
            input_items=[{"role": "user", "content": payload}],
            max_output_tokens=300,
        )
        return result, usage
    except openai.OpenAIError as exc:
        if is_content_filter(exc):
            raise
        log.warning("rewrite.fallback", error_type=type(exc).__name__)
        return fallback_rewrite(message), None
