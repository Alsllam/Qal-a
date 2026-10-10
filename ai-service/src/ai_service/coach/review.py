"""Post-game review: turn engine-found key moments into short, grounded teaching notes.

Engine analysis stays on the device (``game_ai``). This service never searches for moves: it
explains the move played and the engine's best move, using the rules and lessons as sources.
"""

from __future__ import annotations

import json
import uuid
from collections.abc import AsyncIterator

import openai
import structlog

from ai_service.api.errors import AppError
from ai_service.auth import CurrentUser
from ai_service.coach.guard import MoveGuard
from ai_service.coach.schemas import CoachReviewRequest, KeyMoment
from ai_service.llm.gateway import LLMGateway, LLMUsage, StreamDone, StreamEvent, StreamFailedError, TextDelta
from ai_service.llm.prompts import load_prompt
from ai_service.rag.retrieval.context import ContextBundle, assemble_context, extract_citations
from ai_service.rag.retrieval.search import Retriever
from ai_service.rag.retrieval.security import SecurityScope
from ai_service.rag.types import SearchQuery
from ai_service.settings import ModelRole, Settings
from ai_service.sse import SseEvent, sse, sse_error
from ai_service.tokens import TokenCounter
from ai_service.usage.meter import UsageMeter

log = structlog.get_logger(__name__)

MAX_MOMENTS = 3

# What to look up in the rules for each engine tag.
TAG_TOPICS: dict[str, str] = {
    "cut_supply": "supply chain cut capture unsupplied pieces cannot capture",
    "lost_supply": "supply chain unsupplied cannot capture or shoot",
    "lost_well": "wells water points held by supplied piece",
    "missed_well": "wells water points held by supplied piece start of turn",
    "water_race": "water victory 10 water points",
    "missed_capture": "capture supplied pieces may capture",
    "hanging_piece": "capture supplied pieces",
    "amir_danger": "capture the Amir win no check",
    "missed_win": "how to win capture Amir take the Qal'a",
    "qala_threat": "take the Qal'a supplied piece on enemy Qal'a",
    "rami_shot": "Rami shoots 2 squares straight empty square in between",
    "faris_slide": "Faris slides up to 3 squares first enemy piece",
    "ply_limit": "60-ply rule more water wells pieces",
}
DEFAULT_TOPIC = "supply water wells capture"


def select_moments(moments: list[KeyMoment], limit: int = MAX_MOMENTS) -> list[KeyMoment]:
    """The ``limit`` biggest evaluation swings, shown in game order."""
    biggest = sorted(moments, key=lambda m: (-abs(m.swing), m.ply))[:limit]
    return sorted(biggest, key=lambda m: m.ply)


def validate_against_record(req: CoachReviewRequest) -> None:
    """Key moments must match the game record: the coach explains this game, nothing invented."""
    moves = req.record.moves
    for m in req.key_moments:
        if moves and (m.ply > len(moves) or moves[m.ply - 1] != m.move):
            raise AppError("Coach:Errors:MoveMismatch", 400, details=[f"ply {m.ply}: {m.move}"])


def retrieval_query(moment: KeyMoment) -> str:
    topics = [TAG_TOPICS[t] for t in moment.tags if t in TAG_TOPICS]
    return " ".join(topics) or DEFAULT_TOPIC


class CoachService:
    def __init__(
        self,
        *,
        settings: Settings,
        gateway: LLMGateway,
        retriever: Retriever,
        meter: UsageMeter,
        counter: TokenCounter,
    ) -> None:
        self.settings = settings
        self.gateway = gateway
        self.retriever = retriever
        self.meter = meter
        self.counter = counter

    def _moment_payload(self, req: CoachReviewRequest, m: KeyMoment) -> str:
        data = {
            "ply": m.ply,
            "side": m.side,
            "isPlayer": m.side == req.record.player_side,
            "move": m.move,
            "bestMove": m.best_move,
            "evalBefore": round(max(-50.0, min(50.0, m.eval_before)), 2),
            "evalAfter": round(max(-50.0, min(50.0, m.eval_after)), 2),
            "swing": round(m.swing, 2),
            "tags": m.tags,
        }
        game = {
            "rulesVersion": req.record.rules_version,
            "playerSide": req.record.player_side,
            "totalPlies": len(req.record.moves) or None,
            "outcome": req.record.outcome.model_dump() if req.record.outcome else None,
        }
        return (
            f"locale: {req.locale}\nlevel: {req.level}\n\n"
            f"<game>{json.dumps(game, ensure_ascii=False)}</game>\n"
            f"<key_moment>{json.dumps(data, ensure_ascii=False)}</key_moment>\n\n"
            "Explain this key moment."
        )

    async def _open(
        self, req: CoachReviewRequest, m: KeyMoment, scope: SecurityScope
    ) -> tuple[AsyncIterator[StreamEvent], ContextBundle]:
        chunks = await self.retriever.retrieve(
            SearchQuery(text=retrieval_query(m), language="en", top=3), scope
        )
        context = assemble_context(chunks, self.counter, self.settings.coach_context_budget_tokens)
        items: list[dict[str, object]] = []
        if not context.empty:
            items.append({"role": "developer", "content": f"<sources>\n{context.text}\n</sources>"})
        items.append({"role": "user", "content": self._moment_payload(req, m)})
        stream = await self.gateway.open_text_stream(
            ModelRole.CHAT,
            instructions=load_prompt("coach_review", self.settings.coach_prompt_version),
            input_items=items,
            max_output_tokens=self.settings.coach_max_output_tokens,
            temperature=self.settings.chat_temperature,
        )
        return stream, context

    async def stream_review(
        self, user: CurrentUser, scope: SecurityScope, req: CoachReviewRequest
    ) -> AsyncIterator[SseEvent]:
        validate_against_record(req)
        moments = select_moments(req.key_moments)
        review_id = str(uuid.uuid4())
        # Open the first stream before emitting anything, so early failures map to HTTP statuses.
        first_stream, first_ctx = await self._open(req, moments[0], scope)
        yield sse(
            "meta",
            {
                "reviewId": review_id,
                "locale": req.locale,
                "level": req.level,
                "promptVersion": f"coach_review.{self.settings.coach_prompt_version}",
                "moments": [
                    {"index": i, "ply": m.ply, "move": m.move, "bestMove": m.best_move, "side": m.side}
                    for i, m in enumerate(moments)
                ],
            },
        )
        totals = {"inputTokens": 0, "cachedTokens": 0, "outputTokens": 0}
        for index, moment in enumerate(moments):
            try:
                if index == 0:
                    stream, ctx = first_stream, first_ctx
                else:
                    stream, ctx = await self._open(req, moment, scope)
            except (openai.OpenAIError, AppError) as exc:
                yield sse_error(exc, req.locale)
                break
            yield sse("moment_start", {"index": index, "ply": moment.ply})
            guard = MoveGuard({moment.move, moment.best_move})
            parts: list[str] = []
            usage: LLMUsage | None = None
            incomplete = False
            failed: BaseException | None = None
            try:
                async for event in stream:
                    if isinstance(event, TextDelta):
                        safe = guard.feed(event.text)
                        if safe:
                            parts.append(safe)
                            yield sse("delta", {"index": index, "text": safe})
                    elif isinstance(event, StreamDone):
                        usage = event.usage
                        incomplete = event.incomplete
            except (StreamFailedError, openai.OpenAIError) as exc:
                failed = exc
                incomplete = True
            tail = guard.flush()
            if tail:
                parts.append(tail)
                yield sse("delta", {"index": index, "text": tail})
            if guard.removed:
                log.warning("coach.invented_moves_removed", count=len(guard.removed))
            cited = extract_citations("".join(parts), ctx.citations)
            yield sse(
                "moment_end",
                {
                    "index": index,
                    "ply": moment.ply,
                    "incomplete": incomplete,
                    "removedMoves": len(guard.removed),
                    "citations": [c.to_public() for c in cited],
                },
            )
            if usage is not None:
                totals["inputTokens"] += usage.input_tokens
                totals["cachedTokens"] += usage.cached_tokens
                totals["outputTokens"] += usage.output_tokens
                await self.meter.record(
                    tenant_id=user.tenant_id, user_id=user.sub, feature="coach.review", usage=usage
                )
            if failed is not None:
                yield sse_error(
                    failed
                    if isinstance(failed, openai.OpenAIError)
                    else AppError("General:Errors:AiUnavailable", 502),
                    req.locale,
                )
                break
        yield sse("done", {"reviewId": review_id, "usage": totals})
