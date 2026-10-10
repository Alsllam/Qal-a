"""Arabic normalization for the *search copy* of text.

The original text is always kept for display and for the model. Only the ``content_search`` field
and query matching use the normalized copy.
"""

from __future__ import annotations

import re
import unicodedata

TATWEEL = "\u0640"
# Harakat, tanween, shadda, sukun, superscript alef, Quranic marks.
_DIACRITICS = re.compile("[\u0610-\u061a\u064b-\u065f\u0670\u06d6-\u06dc\u06df-\u06e8\u06ea-\u06ed]")
_ALEF = str.maketrans({"أ": "ا", "إ": "ا", "آ": "ا", "ٱ": "ا"})
_YA = str.maketrans({"ى": "ي"})
_DIGITS = str.maketrans(
    {**{chr(0x0660 + i): str(i) for i in range(10)}, **{chr(0x06F0 + i): str(i) for i in range(10)}}
)
_SPACE_BEFORE_PUNCT = re.compile(r"\s+([،؛؟.,;:!?)\]])")
_SPACE_AFTER_OPEN = re.compile(r"([(\[])\s+")
_MULTI_SPACE = re.compile("[ \t\u00a0\u200e\u200f]+")
_ARABIC_LETTER = re.compile("[\u0621-\u064a]")
_LATIN_LETTER = re.compile(r"[A-Za-z]")


def normalize_digits(text: str) -> str:
    """Arabic-Indic and Persian digits → ASCII digits."""
    return text.translate(_DIGITS)


def strip_diacritics(text: str) -> str:
    return _DIACRITICS.sub("", text.replace(TATWEEL, ""))


def normalize_for_search(text: str) -> str:
    """Build the search copy: no tatweel/diacritics, unified alef and ya, ASCII digits, tidy spaces."""
    text = unicodedata.normalize("NFC", text)
    text = strip_diacritics(text)
    text = text.translate(_ALEF).translate(_YA)
    text = normalize_digits(text)
    text = _SPACE_BEFORE_PUNCT.sub(r"\1", text)
    text = _SPACE_AFTER_OPEN.sub(r"\1", text)
    text = _MULTI_SPACE.sub(" ", text)
    return "\n".join(line.strip() for line in text.splitlines()).strip()


def detect_language(text: str) -> str:
    """``ar`` when Arabic letters dominate the letters of the text, otherwise ``en``."""
    arabic = len(_ARABIC_LETTER.findall(text))
    latin = len(_LATIN_LETTER.findall(text))
    return "ar" if arabic > latin else "en"
