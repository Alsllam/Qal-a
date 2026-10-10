from ai_service.safety.pii import redact, redact_processor


def test_redacts_email_phone_and_national_id() -> None:
    text = "Contact sara@example.com or +966 50 123 4567, ID 1234567890."
    out = redact(text)
    assert "sara@example.com" not in out and "[EMAIL]" in out
    assert "[PHONE]" in out
    assert "1234567890" not in out


def test_arabic_indic_digits_are_redacted() -> None:
    assert "[PHONE]" in redact("جوالي ٠٥٠١٢٣٤٥٦٧")


def test_dates_and_moves_are_kept() -> None:
    text = "Rules v0.6 adopted 2026-10-10; play c3xd3 at ply 12."
    assert redact(text) == text


def test_structlog_processor_redacts_nested_values() -> None:
    event = {"event": "x", "data": {"email": "a@b.co", "list": ["0501234567"]}}
    out = redact_processor(None, "info", event)
    assert out["data"] == {"email": "[EMAIL]", "list": ["[PHONE]"]}
