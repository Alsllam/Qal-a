"""``POST /ai-api/chat`` (SSE) and the user's conversation history."""

from __future__ import annotations

from typing import Any, Literal

from fastapi import APIRouter, Query
from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel
from sse_starlette.sse import EventSourceResponse

from ai_service.api.deps import ContainerDep, LimitedUserDep, ScopeDep, UserDep
from ai_service.api.errors import AppError
from ai_service.rag.answer import ChatInput
from ai_service.sse import prime

router = APIRouter(prefix="/ai-api", tags=["chat"])


class _Camel(BaseModel):
    # Unknown fields (e.g. a "filter" or "tenantId" smuggled into the body) are ignored.
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="ignore")


class ChatContext(_Camel):
    screen: str | None = Field(default=None, max_length=64)
    entity_id: str | None = Field(default=None, max_length=64)


class ChatRequest(_Camel):
    conversation_id: str | None = Field(default=None, max_length=36)
    message: str = Field(min_length=1, max_length=2000)
    locale: Literal["ar", "en"] = "en"
    context: ChatContext | None = None


@router.post("/chat")
async def chat(
    body: ChatRequest, user: LimitedUserDep, scope: ScopeDep, container: ContainerDep
) -> EventSourceResponse:
    stream = container.answers.stream_chat(
        user,
        scope,
        ChatInput(
            message=body.message,
            locale=body.locale,
            conversation_id=body.conversation_id,
            screen=body.context.screen if body.context else None,
        ),
    )
    return EventSourceResponse(await prime(stream), ping=15)


@router.get("/conversations")
async def list_conversations(
    user: UserDep,
    container: ContainerDep,
    skip: int = Query(default=0, ge=0),
    take: int = Query(default=20, ge=1, le=100),
) -> dict[str, Any]:
    rows, total = await container.conversations.list_for_user(user.tenant_id, user.sub, skip=skip, take=take)
    return {
        "totalCount": total,
        "items": [
            {
                "id": c.id,
                "title": c.title,
                "locale": c.locale,
                "createdAt": c.created_at.isoformat(),
                "updatedAt": c.updated_at.isoformat(),
            }
            for c in rows
        ],
    }


@router.get("/conversations/{conversation_id}")
async def get_conversation(conversation_id: str, user: UserDep, container: ContainerDep) -> dict[str, Any]:
    conv = await container.conversations.get(user.tenant_id, user.sub, conversation_id)
    if conv is None:
        raise AppError("General:Errors:NotFound", 404)
    return {
        "id": conv.id,
        "title": conv.title,
        "locale": conv.locale,
        "createdAt": conv.created_at.isoformat(),
        "messages": [
            {
                "id": m.id,
                "role": m.role,
                "content": m.content,
                "citations": m.citations,
                "incomplete": m.incomplete,
                "createdAt": m.created_at.isoformat(),
            }
            for m in conv.messages
        ],
    }


@router.delete("/conversations/{conversation_id}", status_code=204)
async def delete_conversation(conversation_id: str, user: UserDep, container: ContainerDep) -> None:
    if not await container.conversations.delete(user.tenant_id, user.sub, conversation_id):
        raise AppError("General:Errors:NotFound", 404)
