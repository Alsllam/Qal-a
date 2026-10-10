from ai_service.rag.ingestion.normalize import (
    detect_language,
    normalize_digits,
    normalize_for_search,
    strip_diacritics,
)

FATHA, KASRA, SHADDA, TATWEEL = chr(0x064E), chr(0x0650), chr(0x0651), chr(0x0640)


def test_removes_tatweel_and_diacritics() -> None:
    word = f"ال{TATWEEL}{TATWEEL}قل{FATHA}ع{SHADDA}ة"
    assert normalize_for_search(word) == "القلعة"
    assert strip_diacritics(f"ب{KASRA}ئ{FATHA}ر") == "بئر"


def test_unifies_alef_and_ya() -> None:
    assert normalize_for_search("أمير إمداد آبار") == "امير امداد ابار"
    assert normalize_for_search("على مستوى") == "علي مستوي"


def test_digits_and_spacing() -> None:
    assert normalize_digits("١٠ نقاط و۳ آبار") == "10 نقاط و3 آبار"
    assert normalize_for_search("كم نقطة ؟  هل  تفوز ، أم لا") == "كم نقطة؟ هل تفوز، ام لا"


def test_original_text_is_untouched_by_caller() -> None:
    original = "الأمير"
    normalize_for_search(original)
    assert original == "الأمير"


def test_detect_language() -> None:
    assert detect_language("كيف يتحرك الفارس؟") == "ar"
    assert detect_language("How does the Faris (فارس) move?") == "en"
