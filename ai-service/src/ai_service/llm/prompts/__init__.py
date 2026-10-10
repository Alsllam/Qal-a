"""Versioned prompt files. A change is a new file (``answer.v2.md``), never an edit of an old one."""

from __future__ import annotations

from functools import lru_cache
from pathlib import Path

_DIR = Path(__file__).parent


@lru_cache(maxsize=32)
def load_prompt(name: str, version: str) -> str:
    """Load ``{name}.{version}.md``. The text is kept byte-for-byte stable for prompt caching."""
    path = _DIR / f"{name}.{version}.md"
    if not path.is_file():
        raise FileNotFoundError(f"Prompt {name}.{version}.md not found")
    return path.read_text(encoding="utf-8")
