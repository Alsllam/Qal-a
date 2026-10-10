"""Treat retrieved text as data: wrap it in ``<source>`` tags that it cannot break out of."""

from __future__ import annotations

import html
import re

_TAG_LIKE = re.compile(r"<\s*/?\s*(source|sources|key_moments|instructions|system)\b[^>]*>", re.I)
_SUSPICIOUS = re.compile(
    r"(ignore (all |the )?(previous|above|prior) (instructions|rules))"
    r"|(disregard (the )?(system|previous) (prompt|instructions))"
    r"|(you are now)|(system prompt)"
    r"|(تجاهل (جميع |كل )?(التعليمات|الأوامر))|(انس (التعليمات|الأوامر))",
    re.I,
)


def neutralize(text: str) -> str:
    """Defuse tags that could close or open a source block inside the text."""
    return _TAG_LIKE.sub(lambda m: m.group(0).replace("<", "‹").replace(">", "›"), text)


def looks_like_injection(text: str) -> bool:
    """Cheap heuristic used for logging/flagging; Prompt Shields is the real check when enabled."""
    return bool(_SUSPICIOUS.search(text))


def _attr(value: str) -> str:
    return html.escape(value.replace("\n", " "), quote=True)


def wrap_source(source_id: str, text: str, *, title: str, path: str, page: int | None) -> str:
    attrs = f'id="{_attr(source_id)}" title="{_attr(title)}"'
    if page is not None:
        attrs += f' page="{page}"'
    if path:
        attrs += f' path="{_attr(path)}"'
    return f"<source {attrs}>\n{neutralize(text)}\n</source>"
