"""The eval datasets stay valid as docs/rules.md evolves."""

import json
import re
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
RULES = (ROOT.parent / "docs" / "rules.md").read_text(encoding="utf-8")
HEADINGS = {m.group(1).strip() for m in re.finditer(r"^#{1,6}\s+(.*)$", RULES, re.M)}


@pytest.mark.parametrize("language", ["en", "ar"])
def test_dataset_is_well_formed(language: str) -> None:
    rows = [
        json.loads(line)
        for line in (ROOT / "evals" / "datasets" / f"qala_rules_{language}.jsonl")
        .read_text("utf-8")
        .splitlines()
        if line.strip()
    ]
    assert len({r["id"] for r in rows}) == len(rows)
    answerable = [r for r in rows if r["answerable"]]
    assert len(answerable) >= 15
    assert len([r for r in rows if not r["answerable"]]) >= 3
    for r in rows:
        assert r["language"] == language
        assert r["reference_answer"]
        for source in r["expected_sources"]:
            assert source["document_id"] == "rules"
            assert source["heading"] in HEADINGS, f"{r['id']}: unknown heading {source['heading']!r}"
    assert all(r["expected_sources"] for r in answerable)
