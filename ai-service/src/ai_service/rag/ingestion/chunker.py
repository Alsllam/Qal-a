"""Heading-aware Markdown chunker.

1. Parse the Markdown into blocks: headings, paragraphs, tables, fenced code/diagrams, list items.
   Tables, fenced blocks and numbered list items are atomic: they are never split.
2. Group blocks into sections by heading; each section knows its ``heading_path``.
3. Pack: small neighbouring sections are merged up to ``max_tokens``; a section larger than
   ``max_tokens`` is split between blocks, with a 10-15% overlap carried into the next chunk.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from enum import StrEnum

from ai_service.tokens import TokenCounter

HEADING_SEP = " › "

_HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")
_FENCE = re.compile(r"^\s*(```|~~~)")
_TABLE = re.compile(r"^\s*\|")
_NUMBERED = re.compile(r"^\s{0,3}(\d+|[٠-٩]+)[.)]\s+")
_BULLET = re.compile(r"^\s{0,3}[-*+]\s+")
_SENTENCE_END = re.compile(r"(?<=[.!?؟。])\s+")


class BlockKind(StrEnum):
    HEADING = "heading"
    PARAGRAPH = "paragraph"
    TABLE = "table"
    FENCE = "fence"
    NUMBERED = "numbered"
    BULLET = "bullet"
    RULE = "rule"


@dataclass(frozen=True)
class Block:
    kind: BlockKind
    text: str
    level: int = 0  # heading level

    @property
    def atomic(self) -> bool:
        return self.kind in (BlockKind.TABLE, BlockKind.FENCE, BlockKind.NUMBERED)


@dataclass
class Section:
    path: list[str]
    blocks: list[Block] = field(default_factory=list)


@dataclass(frozen=True)
class Chunk:
    index: int
    text: str
    heading_path: str
    headings: list[str]
    tokens: int


@dataclass(frozen=True)
class ChunkerConfig:
    min_tokens: int = 400
    max_tokens: int = 800
    overlap_ratio: float = 0.12


def parse_blocks(markdown: str) -> list[Block]:
    lines = markdown.replace("\r\n", "\n").split("\n")
    blocks: list[Block] = []
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        if not line.strip():
            i += 1
            continue
        if _FENCE.match(line):
            marker = _FENCE.match(line).group(1)  # type: ignore[union-attr]
            j = i + 1
            while j < n and not lines[j].strip().startswith(marker):
                j += 1
            blocks.append(Block(BlockKind.FENCE, "\n".join(lines[i : j + 1])))
            i = j + 1
            continue
        heading = _HEADING.match(line)
        if heading:
            blocks.append(Block(BlockKind.HEADING, heading.group(2).strip(), len(heading.group(1))))
            i += 1
            continue
        if line.strip() in ("---", "***", "___"):
            i += 1
            continue
        if _TABLE.match(line):
            j = i
            while j < n and _TABLE.match(lines[j]):
                j += 1
            blocks.append(Block(BlockKind.TABLE, "\n".join(lines[i:j])))
            i = j
            continue
        for kind, pattern in ((BlockKind.NUMBERED, _NUMBERED), (BlockKind.BULLET, _BULLET)):
            if pattern.match(line):
                j = i + 1
                # Continuation lines: indented, non-empty and not a new item/heading/table.
                while (
                    j < n
                    and lines[j].strip()
                    and not _NUMBERED.match(lines[j])
                    and not _BULLET.match(lines[j])
                    and not _HEADING.match(lines[j])
                    and not _TABLE.match(lines[j])
                    and not _FENCE.match(lines[j])
                ):
                    j += 1
                # Nested bullets under a numbered item belong to it.
                if kind is BlockKind.NUMBERED:
                    while j < n and lines[j].startswith(("  ", "\t")) and lines[j].strip():
                        j += 1
                blocks.append(Block(kind, "\n".join(lines[i:j])))
                i = j
                break
        else:
            j = i + 1
            while (
                j < n
                and lines[j].strip()
                and not _HEADING.match(lines[j])
                and not _TABLE.match(lines[j])
                and not _FENCE.match(lines[j])
                and not _NUMBERED.match(lines[j])
                and not _BULLET.match(lines[j])
            ):
                j += 1
            blocks.append(Block(BlockKind.PARAGRAPH, "\n".join(lines[i:j])))
            i = j
    return blocks


def split_sections(blocks: list[Block]) -> list[Section]:
    sections: list[Section] = []
    stack: list[tuple[int, str]] = []
    current = Section(path=[])
    for block in blocks:
        if block.kind is BlockKind.HEADING:
            if current.blocks or current.path:
                sections.append(current)
            while stack and stack[-1][0] >= block.level:
                stack.pop()
            stack.append((block.level, block.text))
            current = Section(path=[t for _, t in stack], blocks=[block])
        else:
            current.blocks.append(block)
    if current.blocks:
        sections.append(current)
    return [s for s in sections if any(b.kind is not BlockKind.HEADING for b in s.blocks)]


def _render(block: Block) -> str:
    if block.kind is BlockKind.HEADING:
        return f"{'#' * block.level} {block.text}"
    return block.text


class MarkdownChunker:
    def __init__(self, counter: TokenCounter, config: ChunkerConfig | None = None) -> None:
        self.counter = counter
        self.config = config or ChunkerConfig()

    def _tokens(self, blocks: list[Block]) -> int:
        return self.counter.count("\n\n".join(_render(b) for b in blocks))

    def _split_oversized_paragraph(self, block: Block) -> list[Block]:
        if block.atomic or self.counter.count(block.text) <= self.config.max_tokens:
            return [block]
        parts: list[Block] = []
        buf: list[str] = []
        for sentence in _SENTENCE_END.split(block.text):
            candidate = " ".join([*buf, sentence])
            if buf and self.counter.count(candidate) > self.config.max_tokens // 2:
                parts.append(Block(block.kind, " ".join(buf)))
                buf = [sentence]
            else:
                buf.append(sentence)
        if buf:
            parts.append(Block(block.kind, " ".join(buf)))
        return parts

    def _overlap(self, blocks: list[Block], chunk_tokens: int) -> list[Block]:
        """Trailing content of a chunk to repeat at the start of the next one (10-15%)."""
        budget = max(1, int(chunk_tokens * self.config.overlap_ratio))
        carried: list[Block] = []
        for block in reversed(blocks):
            if block.kind is BlockKind.HEADING:
                break
            tokens = self.counter.count(block.text)
            used = self._tokens(carried) if carried else 0
            if used + tokens <= budget:
                carried.insert(0, block)
                continue
            if block.kind is BlockKind.PARAGRAPH and not carried:
                # Carry trailing sentences of a long paragraph (never part of an atomic block).
                tail: list[str] = []
                for sentence in reversed(_SENTENCE_END.split(block.text)):
                    if self.counter.count(" ".join([sentence, *tail])) > budget:
                        break
                    tail.insert(0, sentence)
                if tail:
                    carried.insert(0, Block(BlockKind.PARAGRAPH, " ".join(tail)))
            break
        return carried

    def _split_section(self, section: Section) -> list[tuple[list[Block], list[str]]]:
        heading = [b for b in section.blocks if b.kind is BlockKind.HEADING]
        body: list[Block] = []
        for b in section.blocks:
            if b.kind is not BlockKind.HEADING:
                body.extend(self._split_oversized_paragraph(b))
        pieces: list[tuple[list[Block], list[str]]] = []
        current: list[Block] = list(heading)
        has_body = False
        for block in body:
            if has_body and self._tokens([*current, block]) > self.config.max_tokens:
                pieces.append((current, section.path))
                overlap = self._overlap(current, self._tokens(current))
                current = [*heading, *overlap]
                has_body = bool(overlap)
            current.append(block)
            has_body = True
        if has_body:
            pieces.append((current, section.path))
        return pieces

    def chunk(self, markdown: str) -> list[Chunk]:
        sections = split_sections(parse_blocks(markdown))
        pieces: list[tuple[list[Block], list[str]]] = []
        for section in sections:
            pieces.extend(self._split_section(section))

        chunks: list[Chunk] = []
        group: list[tuple[list[Block], list[str]]] = []

        def flush() -> None:
            if not group:
                return
            text = "\n\n".join("\n\n".join(_render(b) for b in blocks) for blocks, _ in group)
            headings: list[str] = []
            for _, path in group:
                for h in path:
                    if h not in headings:
                        headings.append(h)
            chunks.append(
                Chunk(
                    index=len(chunks),
                    text=text,
                    heading_path=HEADING_SEP.join(group[0][1]),
                    headings=headings,
                    tokens=self.counter.count(text),
                )
            )
            group.clear()

        group_tokens = 0
        for blocks, path in pieces:
            tokens = self._tokens(blocks)
            if group and (
                group_tokens >= self.config.min_tokens or group_tokens + tokens > self.config.max_tokens
            ):
                flush()
                group_tokens = 0
            group.append((blocks, path))
            group_tokens += tokens
        flush()
        return chunks
