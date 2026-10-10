"""Request dependencies: container, locale, current user (JWT), rate limit, permissions, scope."""

from __future__ import annotations

from collections.abc import Awaitable, Callable
from typing import Annotated

import httpx
from fastapi import Depends, Request

from ai_service.api.errors import AppError, resolve_locale
from ai_service.auth import AuthError, CurrentUser
from ai_service.container import Container
from ai_service.rag.retrieval.security import SecurityScope


def get_container(request: Request) -> Container:
    container: Container = request.app.state.container
    return container


ContainerDep = Annotated[Container, Depends(get_container)]


def get_locale(request: Request) -> str:
    return resolve_locale(request.headers.get("accept-language"))


LocaleDep = Annotated[str, Depends(get_locale)]


async def get_current_user(request: Request, container: ContainerDep) -> CurrentUser:
    header = request.headers.get("authorization", "")
    scheme, _, token = header.partition(" ")
    if scheme.lower() != "bearer" or not token.strip():
        raise AppError("General:Errors:Unauthorized", 401, headers={"WWW-Authenticate": "Bearer"})
    try:
        return await container.validator.validate(token.strip())
    except AuthError as exc:
        raise AppError(
            "General:Errors:Unauthorized", 401, headers={"WWW-Authenticate": 'Bearer error="invalid_token"'}
        ) from exc
    except httpx.HTTPError as exc:  # JWKS endpoint unreachable
        raise AppError("General:Errors:AiUnavailable", 503) from exc


UserDep = Annotated[CurrentUser, Depends(get_current_user)]


async def rate_limited_user(user: UserDep, container: ContainerDep) -> CurrentUser:
    retry_after = container.limiter.hit(f"{user.tenant_id}:{user.sub}")
    if retry_after is not None:
        raise AppError(
            "General:Errors:TooManyRequests",
            429,
            headers={"Retry-After": str(max(1, int(retry_after + 0.999)))},
        )
    return user


LimitedUserDep = Annotated[CurrentUser, Depends(rate_limited_user)]


def require_permission(setting_name: str) -> Callable[[CurrentUser, Container], Awaitable[CurrentUser]]:
    """Dependency factory; the permission name comes from settings (e.g. ``ingest_permission``)."""

    async def check(user: UserDep, container: ContainerDep) -> CurrentUser:
        permission = str(getattr(container.settings, setting_name))
        if not user.has_permission(permission):
            raise AppError("General:Errors:Forbidden", 403)
        return user

    return check


def security_scope(user: UserDep, container: ContainerDep) -> SecurityScope:
    """Built only from the validated token. Request bodies never reach this."""
    groups = (container.settings.public_acl_group, *(r for r in user.roles if "|" not in r))
    return SecurityScope(tenant_id=user.tenant_id, groups=tuple(dict.fromkeys(groups)))


ScopeDep = Annotated[SecurityScope, Depends(security_scope)]
