"""Security trimming. The scope is built from the validated token, never from the request body."""

from __future__ import annotations

from collections.abc import Iterable
from dataclasses import dataclass

# Fields a caller (or the query-rewrite model) may narrow on. Tenant and ACL are never in this list.
ALLOWED_FILTER_FIELDS = frozenset({"doc_type", "language"})


def odata_literal(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


@dataclass(frozen=True)
class SecurityScope:
    tenant_id: str
    groups: tuple[str, ...]

    def __post_init__(self) -> None:
        if not self.tenant_id:
            raise ValueError("tenant_id is required for retrieval")
        if not self.groups:
            raise ValueError("at least one ACL group is required for retrieval")
        for g in self.groups:
            if "|" in g:
                raise ValueError("ACL group names cannot contain '|'")

    def allows(self, tenant_id: str, acl_groups: Iterable[str]) -> bool:
        return tenant_id == self.tenant_id and bool(set(acl_groups) & set(self.groups))

    def odata_filter(self) -> str:
        groups = "|".join(self.groups)
        return (
            f"tenant_id eq {odata_literal(self.tenant_id)} and "
            f"acl_groups/any(g: search.in(g, {odata_literal(groups)}, '|'))"
        )


def build_filter(scope: SecurityScope, extra: dict[str, str] | None = None) -> str:
    """The security filter, AND-ed with optional whitelisted narrowing filters.

    Extra filters can only narrow results; unknown fields (e.g. ``tenant_id``) raise.
    """
    clauses = [f"({scope.odata_filter()})"]
    for field_name, value in sorted((extra or {}).items()):
        if field_name not in ALLOWED_FILTER_FIELDS:
            raise ValueError(f"Filtering on '{field_name}' is not allowed")
        clauses.append(f"{field_name} eq {odata_literal(value)}")
    return " and ".join(clauses)
