"""Request models for ``POST /ai-api/coach/review`` (camelCase on the wire)."""

from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator
from pydantic.alias_generators import to_camel

MOVE_PATTERN = r"^[a-g][1-7][-x*][a-g][1-7]$"
TAG_PATTERN = r"^[a-z][a-z0-9_]{0,31}$"


class _Camel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="ignore")


class GameOutcome(_Camel):
    winner: Literal["south", "north"] | None = None
    reason: str = Field(default="", max_length=32)


class GameRecord(_Camel):
    rules_version: str = Field(max_length=16)
    moves: list[str] = Field(default_factory=list, max_length=400)
    player_side: Literal["south", "north"]
    outcome: GameOutcome | None = None

    @model_validator(mode="after")
    def _moves_are_notation(self) -> GameRecord:
        import re

        bad = [m for m in self.moves if not re.match(MOVE_PATTERN, m)]
        if bad:
            raise ValueError(f"moves must use Qal'a notation (e.g. c2-c3, c3xd3, d2*d4): {bad[:3]}")
        return self


class KeyMoment(_Camel):
    ply: int = Field(ge=1, le=400)
    move: str = Field(pattern=MOVE_PATTERN)
    best_move: str = Field(pattern=MOVE_PATTERN)
    eval_before: float = Field(ge=-1e6, le=1e6)
    eval_after: float = Field(ge=-1e6, le=1e6)
    tags: list[str] = Field(default_factory=list, max_length=8)

    @model_validator(mode="after")
    def _tags(self) -> KeyMoment:
        import re

        if any(not re.match(TAG_PATTERN, t) for t in self.tags):
            raise ValueError("tags must be snake_case identifiers")
        return self

    @property
    def side(self) -> Literal["south", "north"]:
        return "south" if self.ply % 2 == 1 else "north"

    @property
    def swing(self) -> float:
        clamp = 50.0  # a won position is ±100000 in game_ai; keep swings comparable
        before = max(-clamp, min(clamp, self.eval_before))
        after = max(-clamp, min(clamp, self.eval_after))
        return after - before


class CoachReviewRequest(_Camel):
    record: GameRecord
    key_moments: list[KeyMoment] = Field(min_length=1, max_length=12)
    locale: Literal["ar", "en"] = "en"
    level: Literal["beginner", "intermediate", "advanced"] = "beginner"
