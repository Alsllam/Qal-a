import pytest

from ai_service.tokens import TokenCounter, estimate_tokens


def _broken_loader() -> object:
    raise OSError("network blocked")


def test_falls_back_when_tiktoken_cannot_load() -> None:
    counter = TokenCounter("auto", loader=_broken_loader)  # type: ignore[arg-type]
    assert not counter.exact
    assert counter.count("The Amir is always supplied.") == estimate_tokens("The Amir is always supplied.")


def test_strict_tiktoken_mode_raises() -> None:
    with pytest.raises(OSError):
        TokenCounter("tiktoken", loader=_broken_loader)  # type: ignore[arg-type]


def test_estimate_is_deterministic_and_language_aware() -> None:
    english = "Only supplied pieces may capture or shoot."
    arabic = "لا يأسر ولا يرمي إلا القطع المزوّدة بالماء."
    assert estimate_tokens(english) == estimate_tokens(english)
    assert estimate_tokens(english) == max(round(7 * 1.3 + 0.49), len(english) // 4 + 1)
    assert estimate_tokens(arabic) == -(-len(arabic) * 2 // 7)  # ceil(len / 3.5)
    assert estimate_tokens("") == 0


def test_estimate_mode_never_loads() -> None:
    called = []

    def loader() -> object:
        called.append(1)
        raise AssertionError

    TokenCounter("estimate", loader=loader)  # type: ignore[arg-type]
    assert not called


def test_uses_encoding_when_available() -> None:
    class Fake:
        def encode(self, text: str, *, disallowed_special: tuple[()] = ()) -> list[int]:
            return list(range(len(text.split())))

    counter = TokenCounter("auto", loader=Fake)
    assert counter.exact
    assert counter.count("a b c") == 3
