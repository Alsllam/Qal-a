from pathlib import Path

from ai_service.rag.ingestion.chunker import (
    BlockKind,
    ChunkerConfig,
    MarkdownChunker,
    parse_blocks,
)
from ai_service.tokens import TokenCounter

RULES = Path(__file__).resolve().parents[3] / "docs" / "rules.md"

TABLE = "\n".join(
    ["| القطعة | الحرف | العدد | الحركة |", "|---|---|---|---|"]
    + [
        f"| قطعة رقم {i} | J | {i} | خطوة واحدة إلى الأمام أو الخلف أو الجانب في كل دور من أدوار اللعب |"
        for i in range(40)
    ]
)

ARABIC_DOC = f"""# قواعد قلعة

## الإمداد بالماء

{"لا يأسر ولا يرمي إلا الجندي المزوّد بالماء. تتدفق المياه من القلعة والأمير عبر سلسلة القطع المتلامسة. " * 25}

{"إذا انقطعت السلسلة فإن القطع المعزولة تمشي ولكنها لا تهاجم حتى تعود متصلة. " * 25}

## جدول القطع

{TABLE}

## الفوز

1. أسر الأمير: إذا أسرت أمير الخصم أو رميته فزت.
   - يشمل ذلك الرمي من الرامي.
2. أخذ القلعة: قطعة مزوّدة على قلعة الخصم في نهاية دورك.
3. نصر الماء: عشر نقاط ماء في بداية دورك.
"""


def chunker(min_t: int = 120, max_t: int = 300) -> MarkdownChunker:
    return MarkdownChunker(TokenCounter("estimate"), ChunkerConfig(min_t, max_t, 0.12))


def test_parse_blocks_keeps_tables_and_numbered_items_whole() -> None:
    blocks = parse_blocks(ARABIC_DOC)
    tables = [b for b in blocks if b.kind is BlockKind.TABLE]
    assert len(tables) == 1 and tables[0].text == TABLE
    numbered = [b for b in blocks if b.kind is BlockKind.NUMBERED]
    assert len(numbered) == 3
    assert "يشمل ذلك الرمي" in numbered[0].text  # nested bullet stays with its item


def test_arabic_document_chunks_with_heading_path_and_overlap() -> None:
    chunks = chunker().chunk(ARABIC_DOC)
    assert len(chunks) >= 3
    water = [c for c in chunks if c.heading_path == "قواعد قلعة › الإمداد بالماء"]
    assert len(water) >= 2, "a long section is split"
    for c in water:
        assert c.text.startswith("## الإمداد بالماء"), "continuation chunks repeat the heading"
        assert c.tokens <= 300
    # Overlap: the next chunk starts with text carried from the end of the previous one.
    first_body_end = water[0].text.split("\n\n")[-1][-60:]
    assert first_body_end in water[1].text


def test_table_is_never_split_even_when_oversized() -> None:
    chunks = chunker().chunk(ARABIC_DOC)
    holding = [c for c in chunks if "| قطعة رقم 0 |" in c.text]
    assert len(holding) == 1
    assert TABLE in holding[0].text
    assert all("| قطعة رقم 39 |" not in c.text for c in chunks if c is not holding[0])


def test_numbered_items_are_never_split() -> None:
    for c in chunker(40, 60).chunk(ARABIC_DOC):
        for line in c.text.splitlines():
            if line.startswith("1. أسر الأمير"):
                assert "يشمل ذلك الرمي" in c.text


def test_rules_md_chunks_within_bounds() -> None:
    chunks = MarkdownChunker(TokenCounter("estimate")).chunk(RULES.read_text(encoding="utf-8"))
    assert len(chunks) >= 5
    for c in chunks[:-1]:
        assert 400 <= c.tokens <= 800 + 100, (c.heading_path, c.tokens)
    headings = {h for c in chunks for h in c.headings}
    assert "6. Water: the Supply rule (the heart of the game)" in headings
    assert "11. Rulings and FAQ" in headings
    quick_ref = [c for c in chunks if "QAL'A" in c.text and "PLY 60" in c.text]
    assert len(quick_ref) == 1, "the quick-reference card (a fenced block) is not split"
