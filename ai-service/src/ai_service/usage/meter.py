"""Token and cost accounting per tenant, user, feature and deployment."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime

import structlog
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from ai_service.conversations.models import UsageRecord
from ai_service.llm.gateway import LLMUsage
from ai_service.settings import Price

log = structlog.get_logger(__name__)


def estimate_cost(usage: LLMUsage, price: Price | None) -> float:
    if price is None:
        return 0.0
    uncached = max(0, usage.input_tokens - usage.cached_tokens)
    return (
        uncached * price.input + usage.cached_tokens * price.cached_input + usage.output_tokens * price.output
    ) / 1_000_000


@dataclass(frozen=True)
class UsageSummaryRow:
    feature: str
    deployment: str
    requests: int
    input_tokens: int
    cached_tokens: int
    output_tokens: int
    cost_usd: float


class UsageMeter:
    def __init__(self, sessions: async_sessionmaker[AsyncSession], prices: dict[str, Price]) -> None:
        self.sessions = sessions
        self.prices = prices

    async def record(self, *, tenant_id: str, user_id: str, feature: str, usage: LLMUsage) -> float:
        price = self.prices.get(usage.deployment)
        cost = estimate_cost(usage, price)
        async with self.sessions() as s, s.begin():
            s.add(
                UsageRecord(
                    tenant_id=tenant_id,
                    user_id=user_id,
                    feature=feature,
                    deployment=usage.deployment,
                    input_tokens=usage.input_tokens,
                    cached_tokens=usage.cached_tokens,
                    output_tokens=usage.output_tokens,
                    latency_ms=usage.latency_ms,
                    cost_usd=cost,
                )
            )
        log.info(
            "usage.recorded",
            feature=feature,
            deployment=usage.deployment,
            input_tokens=usage.input_tokens,
            cached_tokens=usage.cached_tokens,
            output_tokens=usage.output_tokens,
            latency_ms=usage.latency_ms,
            cost_usd=round(cost, 6),
            priced=price is not None,
        )
        return cost

    async def summary(self, tenant_id: str, since: datetime) -> list[UsageSummaryRow]:
        async with self.sessions() as s:
            rows = await s.execute(
                select(
                    UsageRecord.feature,
                    UsageRecord.deployment,
                    func.count(),
                    func.sum(UsageRecord.input_tokens),
                    func.sum(UsageRecord.cached_tokens),
                    func.sum(UsageRecord.output_tokens),
                    func.sum(UsageRecord.cost_usd),
                )
                .where(UsageRecord.tenant_id == tenant_id, UsageRecord.created_at >= since)
                .group_by(UsageRecord.feature, UsageRecord.deployment)
            )
            return [
                UsageSummaryRow(f, d, int(n), int(i or 0), int(c or 0), int(o or 0), float(cost or 0))
                for f, d, n, i, c, o, cost in rows.all()
            ]
