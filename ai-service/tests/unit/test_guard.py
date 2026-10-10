from ai_service.coach.guard import REPLACEMENT, MoveGuard


def run(guard: MoveGuard, deltas: list[str]) -> str:
    return "".join(guard.feed(d) for d in deltas) + guard.flush()


def test_allowed_moves_pass_through() -> None:
    guard = MoveGuard({"c3xd3", "d2-d3"})
    text = "You played c3xd3, but d2-d3 kept the chain."
    assert run(guard, [text]) == text
    assert guard.removed == []


def test_invented_move_is_removed_even_when_split_across_deltas() -> None:
    guard = MoveGuard({"c3xd3"})
    out = run(guard, ["Better was e", "2-e", "3 and then c3xd3."])
    assert "e2-e3" not in out
    assert REPLACEMENT in out
    assert "c3xd3" in out
    assert guard.removed == ["e2-e3"]


def test_squares_alone_are_not_moves() -> None:
    guard = MoveGuard(set())
    text = "Hold the Well on c4 and e4."
    assert run(guard, list(text)) == text


def test_arabic_text_streams_unchanged() -> None:
    guard = MoveGuard({"b1-b4"})
    text = "نقلتك b1-b4 قطعت سلسلة الماء عن الفارس."
    assert run(guard, [text[:7], text[7:20], text[20:]]) == text
