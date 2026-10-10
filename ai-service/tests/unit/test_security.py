import pytest

from ai_service.rag.retrieval.security import SecurityScope, build_filter


def test_filter_contains_tenant_and_groups() -> None:
    scope = SecurityScope(tenant_id="qala", groups=("public", "coach"))
    assert build_filter(scope) == (
        "(tenant_id eq 'qala' and acl_groups/any(g: search.in(g, 'public|coach', '|')))"
    )


def test_quotes_are_escaped() -> None:
    scope = SecurityScope(tenant_id="o'brien", groups=("public",))
    assert "tenant_id eq 'o''brien'" in build_filter(scope)


def test_extra_filters_only_narrow_and_cannot_touch_security_fields() -> None:
    scope = SecurityScope(tenant_id="qala", groups=("public",))
    f = build_filter(scope, {"doc_type": "rules"})
    assert f.startswith("(tenant_id eq 'qala'") and f.endswith("and doc_type eq 'rules'")
    for field in ("tenant_id", "acl_groups", "document_id"):
        with pytest.raises(ValueError, match="not allowed"):
            build_filter(scope, {field: "other"})


def test_scope_requires_tenant_and_groups() -> None:
    with pytest.raises(ValueError):
        SecurityScope(tenant_id="", groups=("public",))
    with pytest.raises(ValueError):
        SecurityScope(tenant_id="qala", groups=())
    with pytest.raises(ValueError):
        SecurityScope(tenant_id="qala", groups=("a|b",))


def test_allows() -> None:
    scope = SecurityScope(tenant_id="qala", groups=("public",))
    assert scope.allows("qala", ["public", "admins"])
    assert not scope.allows("qala", ["admins"])
    assert not scope.allows("other", ["public"])
