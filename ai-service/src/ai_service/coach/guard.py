"""Stream filter that removes moves the engine did not report.

The coach may only mention the played move and the engine's best move. Any other move written in
Qal'a notation is replaced before it reaches the player. The filter holds back a short tail of the
stream so a move split across two deltas is still caught.
"""

from __future__ import annotations

import re

_MOVE = re.compile(r"(?<![A-Za-z0-9])[a-g][1-7][-x*][a-g][1-7](?![A-Za-z0-9])")
_HOLD = 6  # longest move token is 5 characters, +1 for the lookahead
REPLACEMENT = "…"


class MoveGuard:
    def __init__(self, allowed: set[str]) -> None:
        self.allowed = allowed
        self.removed: list[str] = []
        self._pending = ""
        self._prev = ""  # last emitted character, for the lookbehind

    def _clean(self, text: str, final: bool) -> tuple[str, str]:
        """Return (safe_to_emit, keep_pending)."""
        full = self._prev + text
        offset = len(self._prev)
        cut = len(full) if final else max(offset, len(full) - _HOLD)
        out: list[str] = []
        pos = offset
        for m in _MOVE.finditer(full):
            if m.start() < offset:
                continue
            if not final and m.end() >= len(full):
                cut = min(cut, m.start())
                break
            if m.start() >= cut:
                break
            out.append(full[pos : m.start()])
            if m.group(0) in self.allowed:
                out.append(m.group(0))
            else:
                self.removed.append(m.group(0))
                out.append(REPLACEMENT)
            pos = m.end()
        cut = max(cut, pos)
        out.append(full[pos:cut])
        return "".join(out), full[cut:]

    def feed(self, delta: str) -> str:
        emitted, self._pending = self._clean(self._pending + delta, final=False)
        if emitted:
            self._prev = emitted[-1]
        return emitted

    def flush(self) -> str:
        emitted, self._pending = self._clean(self._pending, final=True)
        return emitted
