"""Arabic ↔ English game vocabulary.

The rules are written in English with Arabic piece names. Arabic questions are expanded with the
English terms before keyword search (the query-rewrite step also returns English keywords, and
the vector search is cross-lingual; this glossary is the deterministic fallback, and it is what the
offline BM25 eval uses). Keys are in normalized search form (no diacritics, unified alef/ya).
"""

from __future__ import annotations

from ai_service.rag.ingestion.normalize import normalize_for_search

_RAW: dict[str, str] = {
    "قلعة": "qal'a fortress keep",
    "القلعة": "qal'a fortress keep",
    "قلعتي": "qal'a fortress",
    "قلعه": "qal'a fortress",
    "أمير": "amir leader",
    "الأمير": "amir leader",
    "جندي": "jundi soldier",
    "الجندي": "jundi soldier",
    "جنود": "jundi soldier",
    "فارس": "faris horseman slides",
    "الفارس": "faris horseman slides",
    "رامي": "rami archer",
    "الرامي": "rami archer",
    "رمي": "shoot shot rami",
    "يرمي": "shoot shot rami",
    "الرمي": "shoot shot",
    "يطلق": "shoot shot",
    "بئر": "well wells",
    "البئر": "well wells",
    "الآبار": "wells well",
    "آبار": "wells well",
    "البئرين": "wells well",
    "ماء": "water supply",
    "الماء": "water supply",
    "المياه": "water supply",
    "نقاط": "points water",
    "نقطة": "points water",
    "إمداد": "supply supplied",
    "الإمداد": "supply supplied",
    "تموين": "supply supplied",
    "سلسلة": "chain chains touching",
    "السلسلة": "chain chains",
    "متصل": "linked chain touching",
    "متصلة": "linked chain touching",
    "مقطوع": "cut unsupplied",
    "قطع": "cut chain",
    "أسر": "capture captures",
    "يأسر": "capture captures",
    "الأسر": "capture",
    "يفوز": "win wins victory",
    "الفوز": "win victory",
    "فوز": "win victory",
    "تعادل": "draw tie tied",
    "التعادل": "draw tie",
    "دور": "turn turns",
    "الدور": "turn",
    "نقلة": "move ply",
    "نقلات": "moves plies",
    "حركة": "move moves",
    "يتحرك": "moves move step",
    "تتحرك": "moves move step",
    "خطوة": "step",
    "قطري": "diagonal diagonally",
    "قطريًا": "diagonal diagonally",
    "مستقيم": "straight orthogonal",
    "الجنوب": "south",
    "الشمال": "north",
    "أولًا": "first",
    "يبدأ": "first moves starts",
    "تدوين": "notation",
    "الترميز": "notation",
    "رقعة": "board",
    "الرقعة": "board",
    "مربع": "square squares",
    "60": "60-ply ply limit",
    "حد": "limit",
    "إصدار": "version history",
    "الإصدار": "version history",
    "حظ": "luck",
    "تمرير": "pass",
    "يقفز": "jump jumps over",
    "القفز": "jump over",
    "خلف": "behind",
    "يطابق": "disagree",
    "إعداد": "setup",
    "ترتيب": "setup",
    "القطع": "pieces components",
    "مزود": "supplied supply",
    "مزودة": "supplied supply",
    "المزودة": "supplied supply",
    "الخصم": "enemy opponent",
    "خصمي": "enemy opponent",
    "العدو": "enemy",
    "أخذ": "take",
    "يكسب": "earn gain",
    "أكسب": "earn gain",
    "أخسر": "lose lost",
    "يخسر": "lose lost",
    "كش": "check",
    "ينزلق": "slides slide",
    "يرجع": "backwards",
    "الخلف": "backwards behind",
    "مخفية": "hidden see everything",
    "يمر": "pass passes",
    "ينتقل": "flows passes",
    "نهاية": "end",
    "بداية": "start",
    "تعليم": "teaching teach",
    "أعلم": "teaching teach",
}

GLOSSARY: dict[str, str] = {normalize_for_search(k): v for k, v in _RAW.items()}


def expand_arabic(query: str) -> str:
    """English keywords for the Arabic game terms found in ``query`` (empty if none)."""
    terms: list[str] = []
    for word in normalize_for_search(query).replace("؟", " ").split():
        stripped = word.strip(".,،؛:!?()\"'")
        candidates = [stripped]
        for prefix in ("و", "ف", "ب", "ل", "وال", "بال", "لل", "فال"):
            if stripped.startswith(prefix) and len(stripped) > len(prefix) + 2:
                candidates.append(stripped[len(prefix) :])
                candidates.append("ال" + stripped[len(prefix) :])
        for c in candidates:
            hit = GLOSSARY.get(c)
            if hit and hit not in terms:
                terms.append(hit)
                break
    return " ".join(terms)
