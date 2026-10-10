"""The monorepo's own knowledge: ``docs/rules.md`` and ``docs/lessons/*.md``."""

from __future__ import annotations

from pathlib import Path

from ai_service.rag.types import SourceDocument
from ai_service.settings import Settings


def _title(markdown: str, fallback: str) -> str:
    for line in markdown.splitlines():
        if line.startswith("# "):
            return line[2:].strip()
    return fallback


def repo_documents(
    repo_root: Path, settings: Settings, *, tenant_id: str | None = None
) -> list[SourceDocument]:
    docs_dir = repo_root / "docs"
    paths = [docs_dir / "rules.md", *sorted((docs_dir / "lessons").glob("*.md"))]
    out: list[SourceDocument] = []
    for path in paths:
        if not path.is_file():
            continue
        markdown = path.read_text(encoding="utf-8")
        rel = path.relative_to(repo_root).as_posix()
        is_rules = path.name == "rules.md"
        out.append(
            SourceDocument(
                tenant_id=tenant_id or settings.default_tenant_id,
                document_id="rules" if is_rules else f"lessons/{path.stem}",
                title=_title(markdown, path.stem),
                markdown=markdown,
                acl_groups=(settings.public_acl_group,),
                doc_type="rules" if is_rules else "lesson",
                source_url=rel,
                version="0.6" if is_rules else "",
            )
        )
    return out
