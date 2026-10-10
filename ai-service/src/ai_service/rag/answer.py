"""Chat orchestration: rewrite → retrieve → assemble context → stream the grounded answer."""

from __future__ import annotations

from collections.abc import AsyncIterator
from dataclasses import dataclass

import openai
import structlog

from ai_service.api.errors import AppError
from ai_service.auth import CurrentUser
from ai_service.conversations.repository import ConversationRepository
from ai_service.llm.gateway import LLMGateway, LLMUsage, StreamDone, StreamEvent, StreamFailedError, TextDelta
from ai_service.llm.prompts import load_prompt
from ai_service.rag.retrieval.context import ContextBundle, assemble_context, extract_citations
from ai_service.rag.retrieval.rewrite import rewrite_query
from ai_service.rag.retrieval.search import Retriever
from ai_service.rag.retrieval.security import SecurityScope
from ai_service.rag.types import RetrievedChunk, SearchQuery
from ai_service.safety.injection import looks_like_injection
from ai_service.safety.prompt_shields import PromptShields
from ai_service.settings import ModelRole, Settings
from ai_service.sse import SseEvent, sse, sse_error
from ai_service.tokens import TokenCounter
from ai_service.usage.meter import UsageMeter

log = structlog.get_logger(__name__)

NO_ANSWER: dict[str, str] = {
    "en": (
        "I couldn't find this in the official Qal'a rules, so I won't guess. "
        "Try asking about supply and water, the Wells, how each piece moves, or how to win."
    ),
    "ar": (
        "لم أجد هذا في القواعد الرسمية لقلعة، ولن أخمّن. "
        "جرّب السؤال عن الإمداد والماء، أو الآبار، أو حركة كل قطعة، أو طرق الفوز."
    ),
}

CONTEXT_CHUNKS = 6


@dataclass(frozen=True)
class ChatInput:
    message: str
    locale: str
    conversation_id: str | None = None
    screen: str | None = None


def _is_context_length(exc: BaseException) -> bool:
    return isinstance(exc, openai.BadRequestError) and getattr(exc, "code", None) == "context_length_exceeded"


class AnswerService:
    def __init__(
        self,
        *,
        settings: Settings,
        gateway: LLMGateway,
        retriever: Retriever,
        conversations: ConversationRepository,
        meter: UsageMeter,
        counter: TokenCounter,
        shields: PromptShields | None = None,
    ) -> None:
        self.settings = settings
        self.gateway = gateway
        self.retriever = retriever
        self.conversations = conversations
        self.meter = meter
        self.counter = counter
        self.shields = shields

    async def _meter(self, user: CurrentUser, feature: str, usage: LLMUsage | None) -> None:
        if usage is not None:
            await self.meter.record(tenant_id=user.tenant_id, user_id=user.sub, feature=feature, usage=usage)

    async def _shield(self, message: str, chunks: list[RetrievedChunk]) -> list[RetrievedChunk]:
        if self.shields is None:
            return chunks
        result = await self.shields.check(message, [c.content for c in chunks])
        if result.user_attack:
            raise AppError("General:Errors:ContentBlocked", 400)
        kept = [
            c
            for c, bad in zip(chunks, result.document_attacks or [False] * len(chunks), strict=False)
            if not bad
        ]
        if len(kept) < len(chunks):
            log.warning("safety.indirect_injection_dropped", dropped=len(chunks) - len(kept))
        return kept

    def _input_items(
        self, context: ContextBundle, history: list[tuple[str, str]], message: str, language: str
    ) -> list[dict[str, object]]:
        # Caching order: static instructions (separate param) → sources → conversation → question.
        items: list[dict[str, object]] = []
        if not context.empty:
            items.append({"role": "developer", "content": f"<sources>\n{context.text}\n</sources>"})
        for role, text in history:
            items.append({"role": "assistant" if role == "assistant" else "user", "content": text})
        items.append({"role": "user", "content": f"locale: {language}\n\n{message}"})
        return items

    async def _open(
        self, chunks: list[RetrievedChunk], history: list[tuple[str, str]], message: str, language: str
    ) -> tuple[AsyncIterator[StreamEvent], ContextBundle]:
        instructions = load_prompt("answer", self.settings.answer_prompt_version)
        budget = self.settings.context_budget_tokens
        for attempt in range(2):
            context = assemble_context(chunks, self.counter, budget)
            try:
                stream = await self.gateway.open_text_stream(
                    ModelRole.CHAT,
                    instructions=instructions,
                    input_items=self._input_items(context, history, message, language),
                    max_output_tokens=self.settings.chat_max_output_tokens,
                    temperature=self.settings.chat_temperature,
                )
                return stream, context
            except openai.BadRequestError as exc:
                if attempt == 0 and _is_context_length(exc):
                    log.warning("chat.context_too_long_retry")
                    budget //= 2
                    history = history[-2:]
                    continue
                raise
        raise AssertionError("unreachable")  # pragma: no cover

    async def stream_chat(
        self, user: CurrentUser, scope: SecurityScope, req: ChatInput
    ) -> AsyncIterator[SseEvent]:
        history: list[tuple[str, str]] = []
        if req.conversation_id:
            if not await self.conversations.exists(user.tenant_id, user.sub, req.conversation_id):
                raise AppError("General:Errors:NotFound", 404)
            history = await self.conversations.recent_turns(req.conversation_id, self.settings.history_turns)

        rewrite, rewrite_usage = await rewrite_query(
            self.gateway, req.message, history, version=self.settings.rewrite_prompt_version
        )
        await self._meter(user, "chat.rewrite", rewrite_usage)
        language = rewrite.language
        if looks_like_injection(req.message):
            log.info("safety.injection_pattern_in_question")

        chunks: list[RetrievedChunk] = []
        if rewrite.needs_retrieval:
            extra = rewrite.search_query_en if rewrite.search_query_en != rewrite.standalone_query else ""
            chunks = await self.retriever.retrieve(
                SearchQuery(
                    text=rewrite.standalone_query, language=language, extra_keywords=extra, top=CONTEXT_CHUNKS
                ),
                scope,
            )
            chunks = await self._shield(req.message, chunks)

        conversation_id = req.conversation_id
        no_answer = rewrite.needs_retrieval and not chunks
        stream: AsyncIterator[StreamEvent] | None = None
        context = ContextBundle(text="")
        if not no_answer:
            stream, context = await self._open(chunks, history, req.message, language)

        if conversation_id is None:
            conversation_id = await self.conversations.create(
                user.tenant_id, user.sub, title=req.message[:80], locale=language
            )
        await self.conversations.add_message(conversation_id, "user", req.message)
        yield sse(
            "meta",
            {
                "conversationId": conversation_id,
                "language": language,
                "promptVersion": f"answer.{self.settings.answer_prompt_version}",
                "sources": len(context.citations),
            },
        )

        if stream is None:
            text = NO_ANSWER.get(language, NO_ANSWER["en"])
            yield sse("delta", {"text": text})
            message_id = await self.conversations.add_message(
                conversation_id, "assistant", text, prompt_version="no_answer"
            )
            yield sse("citations", {"citations": []})
            yield sse("done", {"messageId": message_id, "incomplete": False, "noAnswer": True, "usage": None})
            return

        parts: list[str] = []
        usage: LLMUsage | None = None
        incomplete = False
        failure: BaseException | None = None
        try:
            async for event in stream:
                if isinstance(event, TextDelta):
                    parts.append(event.text)
                    yield sse("delta", {"text": event.text})
                elif isinstance(event, StreamDone):
                    usage = event.usage
                    incomplete = event.incomplete
        except (StreamFailedError, openai.OpenAIError) as exc:
            failure = exc
            incomplete = True
            log.warning("chat.stream_failed", error_type=type(exc).__name__)

        answer = "".join(parts)
        cited = extract_citations(answer, context.citations)
        public = [c.to_public() for c in cited]
        message_id = await self.conversations.add_message(
            conversation_id,
            "assistant",
            answer,
            citations=public,
            incomplete=incomplete,
            prompt_version=f"answer.{self.settings.answer_prompt_version}",
        )
        await self._meter(user, "chat.answer", usage)
        if failure is not None:
            yield sse_error(
                AppError("General:Errors:ContentBlocked", 400)
                if isinstance(failure, StreamFailedError) and failure.code == "content_filter"
                else failure
                if isinstance(failure, openai.OpenAIError)
                else AppError("General:Errors:AiUnavailable", 502),
                req.locale,
            )
        yield sse("citations", {"citations": public})
        yield sse(
            "done",
            {
                "messageId": message_id,
                "incomplete": incomplete,
                "noAnswer": False,
                "usage": None
                if usage is None
                else {
                    "inputTokens": usage.input_tokens,
                    "cachedTokens": usage.cached_tokens,
                    "outputTokens": usage.output_tokens,
                },
            },
        )
