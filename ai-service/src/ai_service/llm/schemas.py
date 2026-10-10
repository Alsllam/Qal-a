"""Pydantic models used as structured-output JSON schemas (``text.format`` with ``strict: true``)."""

from __future__ import annotations

from typing import Any, Literal

from pydantic import BaseModel, Field


class QueryRewrite(BaseModel):
    """Output of the query-rewrite step (``fast`` model)."""

    standalone_query: str = Field(description="The question rewritten to stand alone, in its language")
    search_query_en: str = Field(
        description="The same question as English search keywords (the rules are written in English)"
    )
    language: Literal["ar", "en"]
    needs_retrieval: bool = Field(description="False only for greetings and small talk")
    topic: Literal["rules", "strategy", "lesson", "other"] | None


class GroundedAnswer(BaseModel):
    answer: str
    citations: list[str]
    confidence: Literal["high", "medium", "low"]
    follow_up_questions: list[str] = Field(max_length=3)


class GroundednessGrade(BaseModel):
    """Rubric grade produced by the ``reasoning`` model in evals."""

    grounded: bool
    unsupported_claims: list[str]
    refused: bool
    explanation: str


def _strictify(node: Any) -> Any:
    if isinstance(node, list):
        return [_strictify(v) for v in node]
    if not isinstance(node, dict):
        return node
    out: dict[str, Any] = {}
    for key, value in node.items():
        if key in ("default", "title", "maxItems", "minItems", "maxLength", "minLength"):
            # Strict mode rejects some keywords; Pydantic still validates them after parsing.
            continue
        if key in ("properties", "$defs"):
            out[key] = {name: _strictify(sub) for name, sub in value.items()}
        else:
            out[key] = _strictify(value)
    if out.get("type") == "object" and "properties" in out:
        out["additionalProperties"] = False
        out["required"] = list(out["properties"].keys())
    return out


def strict_json_schema(model: type[BaseModel]) -> dict[str, Any]:
    """A JSON schema accepted by strict structured outputs (all fields required, no extras)."""
    schema: dict[str, Any] = _strictify(model.model_json_schema())
    return schema
