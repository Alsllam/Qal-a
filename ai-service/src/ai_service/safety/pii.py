"""PII redaction for logs and traces (emails, phone numbers, national IDs, card-like numbers)."""

from __future__ import annotations

import re
from collections.abc import Mapping, MutableMapping
from typing import Any

from ai_service.rag.ingestion.normalize import normalize_digits

_EMAIL = re.compile(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")
# Saudi/Gulf national IDs and iqamas: 10 digits starting with 1 or 2.
_NATIONAL_ID = re.compile(r"(?<!\d)[12]\d{9}(?!\d)")
# International or local phone numbers: +966 5x xxx xxxx, 05xxxxxxxx, (555) 123-4567, ...
_PHONE = re.compile(r"(?<![\w])(?:\+|00)?\d[\d\s().-]{7,}\d(?![\w])")
_CARD = re.compile(r"(?<!\d)(?:\d[ -]?){13,19}(?!\d)")

REDACTED_EMAIL = "[EMAIL]"
REDACTED_ID = "[NATIONAL_ID]"
REDACTED_PHONE = "[PHONE]"
REDACTED_CARD = "[NUMBER]"


def redact(text: str) -> str:
    """Replace personal data with placeholders. Arabic-Indic digits are matched too."""
    text = normalize_digits(text)
    text = _EMAIL.sub(REDACTED_EMAIL, text)
    text = _NATIONAL_ID.sub(REDACTED_ID, text)
    text = _CARD.sub(lambda m: REDACTED_CARD if _luhn(m.group(0)) else m.group(0), text)
    return _PHONE.sub(_phone_or_keep, text)


def _phone_or_keep(match: re.Match[str]) -> str:
    digits = sum(c.isdigit() for c in match.group(0))
    return REDACTED_PHONE if 9 <= digits <= 15 else match.group(0)


def _luhn(candidate: str) -> bool:
    digits = [int(c) for c in candidate if c.isdigit()]
    total = 0
    for i, digit in enumerate(reversed(digits)):
        doubled = digit * 2
        total += (doubled - 9 if doubled > 9 else doubled) if i % 2 == 1 else digit
    return total % 10 == 0


def _redact_value(value: Any) -> Any:
    if isinstance(value, str):
        return redact(value)
    if isinstance(value, Mapping):
        return {k: _redact_value(v) for k, v in value.items()}
    if isinstance(value, list | tuple):
        return [_redact_value(v) for v in value]
    return value


def redact_processor(
    _logger: Any, _method: str, event_dict: MutableMapping[str, Any]
) -> MutableMapping[str, Any]:
    """structlog processor: redact every string value in the event."""
    for key, value in list(event_dict.items()):
        event_dict[key] = _redact_value(value)
    return event_dict
