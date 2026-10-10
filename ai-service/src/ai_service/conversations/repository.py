"""Conversation storage. Every query is scoped to the caller's tenant and user."""

from __future__ import annotations

from datetime import timedelta
from typing import Any

from sqlalchemy import delete, func, select
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from ai_service.conversations.models import Conversation, Message, utcnow


class ConversationRepository:
    def __init__(self, sessions: async_sessionmaker[AsyncSession]) -> None:
        self.sessions = sessions

    async def create(
        self, tenant_id: str, user_id: str, *, title: str, locale: str, feature: str = "chat"
    ) -> str:
        async with self.sessions() as s, s.begin():
            conv = Conversation(
                tenant_id=tenant_id, user_id=user_id, title=title[:200], locale=locale, feature=feature
            )
            s.add(conv)
            await s.flush()
            return conv.id

    async def get(self, tenant_id: str, user_id: str, conversation_id: str) -> Conversation | None:
        async with self.sessions() as s:
            conv = await s.get(Conversation, conversation_id)
            if conv is None or conv.tenant_id != tenant_id or conv.user_id != user_id:
                return None
            await s.refresh(conv, ["messages"])
            return conv

    async def exists(self, tenant_id: str, user_id: str, conversation_id: str) -> bool:
        async with self.sessions() as s:
            found = await s.scalar(
                select(func.count())
                .select_from(Conversation)
                .where(
                    Conversation.id == conversation_id,
                    Conversation.tenant_id == tenant_id,
                    Conversation.user_id == user_id,
                )
            )
            return bool(found)

    async def list_for_user(
        self, tenant_id: str, user_id: str, *, skip: int = 0, take: int = 20
    ) -> tuple[list[Conversation], int]:
        async with self.sessions() as s:
            where = (Conversation.tenant_id == tenant_id, Conversation.user_id == user_id)
            total = await s.scalar(select(func.count()).select_from(Conversation).where(*where)) or 0
            rows = await s.scalars(
                select(Conversation)
                .where(*where)
                .order_by(Conversation.updated_at.desc())
                .offset(skip)
                .limit(take)
            )
            return list(rows), int(total)

    async def delete(self, tenant_id: str, user_id: str, conversation_id: str) -> bool:
        async with self.sessions() as s, s.begin():
            conv = await s.get(Conversation, conversation_id)
            if conv is None or conv.tenant_id != tenant_id or conv.user_id != user_id:
                return False
            await s.execute(delete(Message).where(Message.conversation_id == conversation_id))
            await s.delete(conv)
            return True

    async def add_message(
        self,
        conversation_id: str,
        role: str,
        content: str,
        *,
        citations: list[dict[str, Any]] | None = None,
        incomplete: bool = False,
        prompt_version: str | None = None,
    ) -> str:
        async with self.sessions() as s, s.begin():
            msg = Message(
                conversation_id=conversation_id,
                role=role,
                content=content,
                citations=citations or [],
                incomplete=incomplete,
                prompt_version=prompt_version,
            )
            s.add(msg)
            conv = await s.get(Conversation, conversation_id)
            if conv is not None:
                conv.updated_at = utcnow()
            await s.flush()
            return msg.id

    async def recent_turns(self, conversation_id: str, turns: int) -> list[tuple[str, str]]:
        """The last ``turns`` user/assistant pairs, oldest first (complete messages only)."""
        async with self.sessions() as s:
            rows = await s.scalars(
                select(Message)
                .where(Message.conversation_id == conversation_id, Message.incomplete.is_(False))
                .order_by(Message.created_at.desc())
                .limit(turns * 2)
            )
            return [(m.role, m.content) for m in reversed(list(rows))]

    async def purge_older_than(self, days: int) -> int:
        """Retention: delete conversations not updated for ``days`` days."""
        cutoff = utcnow() - timedelta(days=days)
        async with self.sessions() as s, s.begin():
            ids = list(await s.scalars(select(Conversation.id).where(Conversation.updated_at < cutoff)))
            if ids:
                await s.execute(delete(Message).where(Message.conversation_id.in_(ids)))
                await s.execute(delete(Conversation).where(Conversation.id.in_(ids)))
            return len(ids)
