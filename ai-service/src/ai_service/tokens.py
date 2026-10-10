"""Token counting for budgets and chunking.

Uses tiktoken (``o200k_base``) when its encoding can be loaded. tiktoken downloads the BPE file
on first use, which fails in locked-down networks (and must never happen in tests), so the counter
falls back to a deterministic estimate. The estimate errs on the high side, which keeps
budgets safe.
"""

from __future__ import annotations

import math
import re
from collections.abc import Callable
from typing import Literal, Protocol

import structlog

log = structlog.get_logger(__name__)

_ARABIC = re.compile("[\u0600-\u06ff\u0750-\u077f\u08a0-\u08ff\ufb50-\ufdff\ufe70-\ufeff]")
_WORD = re.compile(r"\S+")


class _Encoding(Protocol):
    def encode(self, text: str, *, disallowed_special: tuple[()] = ...) -> list[int]: ...


def estimate_tokens(text: str) -> int:
    """Deterministic estimate: ~3.5 chars/token for Arabic-heavy text, ~1.3 tokens/word otherwise."""
    if not text:
        return 0
    non_space = sum(1 for c in text if not c.isspace())
    arabic = len(_ARABIC.findall(text))
    if non_space and arabic / non_space >= 0.3:
        return math.ceil(len(text) / 3.5)
    words = len(_WORD.findall(text))
    # Tables, code and diagrams have few "words" but many symbols: never go below chars/4.
    return max(math.ceil(words * 1.3), math.ceil(len(text) / 4))


def _load_tiktoken() -> _Encoding:
    import tiktoken

    enc: _Encoding = tiktoken.get_encoding("o200k_base")
    return enc


class TokenCounter:
    """Counts tokens with tiktoken when available, otherwise with :func:`estimate_tokens`."""

    def __init__(
        self,
        mode: Literal["auto", "tiktoken", "estimate"] = "auto",
        loader: Callable[[], _Encoding] = _load_tiktoken,
    ) -> None:
        self._encoding: _Encoding | None = None
        if mode == "estimate":
            return
        try:
            self._encoding = loader()
        except Exception as exc:  # network blocked, missing cache, ...
            if mode == "tiktoken":
                raise
            log.warning("tokens.tiktoken_unavailable", error_type=type(exc).__name__)

    @property
    def exact(self) -> bool:
        return self._encoding is not None

    def count(self, text: str) -> int:
        if self._encoding is not None:
            return len(self._encoding.encode(text, disallowed_special=()))
        return estimate_tokens(text)
