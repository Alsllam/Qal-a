"""Per-user sliding-window rate limit.

In-process: correct for one replica. With several replicas, move the window to Redis (same
interface) so the limit is shared.
"""

from __future__ import annotations

import time
from collections import deque
from collections.abc import Callable


class SlidingWindowLimiter:
    def __init__(self, limit: int, window_s: float, clock: Callable[[], float] = time.monotonic) -> None:
        self.limit = limit
        self.window = window_s
        self.clock = clock
        self._hits: dict[str, deque[float]] = {}

    def hit(self, key: str) -> float | None:
        """Record a request. Returns ``None`` if allowed, else seconds until the next slot."""
        now = self.clock()
        q = self._hits.setdefault(key, deque())
        while q and now - q[0] >= self.window:
            q.popleft()
        if len(q) >= self.limit:
            return max(0.0, self.window - (now - q[0]))
        q.append(now)
        return None
