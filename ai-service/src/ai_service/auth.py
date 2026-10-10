"""OpenIddict access-token validation (JWKS from the auth host, audience ``ai-api``).

OpenIddict encrypts access tokens by default; the ``ai-api`` resource must be configured to issue
signed, unencrypted JWTs (``DisableAccessTokenEncryption``) or this service cannot read them.
"""

from __future__ import annotations

import asyncio
import time
from dataclasses import dataclass, field
from typing import Any

import httpx
import jwt
import structlog

from ai_service.settings import Settings

log = structlog.get_logger(__name__)


class AuthError(Exception):
    pass


@dataclass(frozen=True)
class CurrentUser:
    sub: str
    tenant_id: str
    roles: tuple[str, ...] = ()
    permissions: frozenset[str] = frozenset()
    token: str = field(default="", repr=False)  # forwarded to .NET APIs for server-side tools

    def has_permission(self, permission: str) -> bool:
        return permission in self.permissions


class JwksCache:
    """Caches signing keys; refetches on an unknown ``kid`` (at most every ``min_refresh_s``)."""

    def __init__(
        self, http: httpx.AsyncClient, url: str, *, ttl_s: int = 3600, min_refresh_s: float = 30.0
    ) -> None:
        self._http = http
        self._url = url
        self._ttl = ttl_s
        self._min_refresh = min_refresh_s
        self._keys: dict[str, Any] = {}
        self._fetched_at = 0.0
        self._lock = asyncio.Lock()

    async def _refresh(self) -> None:
        if not self._url:
            raise AuthError("JWT_JWKS_URL is not configured")
        response = await self._http.get(self._url, timeout=10.0)
        response.raise_for_status()
        keys: dict[str, Any] = {}
        for jwk in response.json().get("keys", []):
            if jwk.get("use", "sig") != "sig":
                continue
            try:
                keys[str(jwk.get("kid", ""))] = jwt.PyJWK(jwk).key
            except jwt.PyJWKError:
                continue
        self._keys = keys
        self._fetched_at = time.monotonic()

    async def get(self, kid: str) -> Any:
        now = time.monotonic()
        if kid in self._keys and now - self._fetched_at < self._ttl:
            return self._keys[kid]
        async with self._lock:
            stale = now - self._fetched_at >= self._ttl
            if (kid not in self._keys and now - self._fetched_at >= self._min_refresh) or stale:
                await self._refresh()
        if kid not in self._keys:
            raise AuthError("unknown signing key")
        return self._keys[kid]


def _as_list(value: Any) -> list[str]:
    if value is None:
        return []
    if isinstance(value, str):
        return [value]
    if isinstance(value, list | tuple):
        return [str(v) for v in value]
    return []


class TokenValidator:
    def __init__(self, settings: Settings, jwks: JwksCache) -> None:
        self.settings = settings
        self.jwks = jwks

    async def validate(self, token: str) -> CurrentUser:
        try:
            header = jwt.get_unverified_header(token)
        except jwt.PyJWTError as exc:
            raise AuthError("malformed token") from exc
        alg = header.get("alg")
        if alg not in self.settings.jwt_algorithms:
            raise AuthError("algorithm not allowed")
        key = await self.jwks.get(str(header.get("kid", "")))
        try:
            claims: dict[str, Any] = jwt.decode(
                token,
                key=key,
                algorithms=self.settings.jwt_algorithms,
                audience=self.settings.jwt_audience,
                issuer=self.settings.jwt_issuer or None,
                leeway=self.settings.jwt_leeway_s,
                options={"require": ["exp", "sub", "aud"]},
            )
        except jwt.PyJWTError as exc:
            log.info("auth.rejected", reason=type(exc).__name__)
            raise AuthError(type(exc).__name__) from exc
        roles = _as_list(claims.get(self.settings.roles_claim)) or _as_list(claims.get("roles"))
        permissions = _as_list(claims.get(self.settings.permissions_claim))
        tenant = claims.get(self.settings.tenant_claim) or self.settings.default_tenant_id
        return CurrentUser(
            sub=str(claims["sub"]),
            tenant_id=str(tenant),
            roles=tuple(roles),
            permissions=frozenset(permissions),
            token=token,
        )
