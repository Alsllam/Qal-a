"""``POST /ai-api/coach/review`` (SSE): explain engine-found key moments of a finished game."""

from __future__ import annotations

from fastapi import APIRouter
from sse_starlette.sse import EventSourceResponse

from ai_service.api.deps import ContainerDep, LimitedUserDep, ScopeDep
from ai_service.coach.schemas import CoachReviewRequest
from ai_service.sse import prime

router = APIRouter(prefix="/ai-api/coach", tags=["coach"])


@router.post("/review")
async def review(
    body: CoachReviewRequest, user: LimitedUserDep, scope: ScopeDep, container: ContainerDep
) -> EventSourceResponse:
    return EventSourceResponse(await prime(container.coach.stream_review(user, scope, body)), ping=15)
