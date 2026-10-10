import time

import httpx
import pytest
import respx

from ai_service.auth import AuthError, JwksCache, TokenValidator
from ai_service.settings import Settings
from tests.helpers import JWKS_URL, KeyPair


@pytest.fixture
async def validator(settings: Settings, mock_http: respx.MockRouter) -> TokenValidator:
    return TokenValidator(settings, JwksCache(httpx.AsyncClient(), JWKS_URL))


async def test_valid_token_extracts_claims(validator: TokenValidator, keys: KeyPair) -> None:
    user = await validator.validate(
        keys.token(
            sub="u-42", tenant_id="t-1", role="coach", permission=["Permissions.Content.ManageLessons"]
        )
    )
    assert user.sub == "u-42"
    assert user.tenant_id == "t-1"
    assert user.roles == ("coach",)
    assert user.has_permission("Permissions.Content.ManageLessons")


async def test_missing_tenant_falls_back_to_default(validator: TokenValidator, keys: KeyPair) -> None:
    user = await validator.validate(keys.token(tenant_id=None))
    assert user.tenant_id == "qala"


async def test_wrong_audience_is_rejected(validator: TokenValidator, keys: KeyPair) -> None:
    with pytest.raises(AuthError):
        await validator.validate(keys.token(aud="players-api"))


async def test_expired_token_is_rejected(validator: TokenValidator, keys: KeyPair) -> None:
    past = int(time.time()) - 3600
    with pytest.raises(AuthError):
        await validator.validate(keys.token(iat=past - 600, exp=past))


async def test_wrong_issuer_is_rejected(validator: TokenValidator, keys: KeyPair) -> None:
    with pytest.raises(AuthError):
        await validator.validate(keys.token(iss="https://evil.test/"))


async def test_token_signed_by_another_key_is_rejected(validator: TokenValidator) -> None:
    other = KeyPair(kid="test-key")  # same kid, different key
    with pytest.raises(AuthError):
        await validator.validate(other.token())


async def test_unknown_kid_refetches_once_then_fails(
    validator: TokenValidator, mock_http: respx.MockRouter
) -> None:
    with pytest.raises(AuthError):
        await validator.validate(KeyPair(kid="rotated").token())
    assert mock_http.routes[0].call_count == 1


async def test_alg_none_is_rejected(validator: TokenValidator) -> None:
    import jwt

    token = jwt.encode({"sub": "x", "aud": "ai-api", "exp": int(time.time()) + 60}, key="", algorithm="none")
    with pytest.raises(AuthError):
        await validator.validate(token)
