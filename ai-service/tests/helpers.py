"""Test helpers: RSA-signed tokens, Responses API SSE bodies, SSE parsing."""

from __future__ import annotations

import json
import time
import uuid
from typing import Any

import jwt
from cryptography.hazmat.primitives.asymmetric import rsa

AUTH_ISSUER = "https://auth.test/"
JWKS_URL = "https://auth.test/.well-known/jwks"
OPENAI_ENDPOINT = "https://unit-test-resource.openai.azure.com"
OPENAI_BASE = f"{OPENAI_ENDPOINT}/openai/v1/"


class KeyPair:
    def __init__(self, kid: str = "test-key") -> None:
        self.kid = kid
        self.private = rsa.generate_private_key(public_exponent=65537, key_size=2048)

    def jwks(self) -> dict[str, Any]:
        jwk = json.loads(jwt.algorithms.RSAAlgorithm.to_jwk(self.private.public_key()))
        jwk.update({"kid": self.kid, "use": "sig", "alg": "RS256"})
        return {"keys": [jwk]}

    def token(self, **overrides: Any) -> str:
        now = int(time.time())
        claims: dict[str, Any] = {
            "sub": "player-1",
            "aud": "ai-api",
            "iss": AUTH_ISSUER,
            "iat": now,
            "exp": now + 600,
            "tenant_id": "qala",
            "role": ["player"],
            "permission": [],
            "jti": str(uuid.uuid4()),
        }
        claims.update(overrides)
        claims = {k: v for k, v in claims.items() if v is not None}
        return jwt.encode(claims, self.private, algorithm="RS256", headers={"kid": self.kid})


def responses_sse(deltas: list[str], *, usage: dict[str, Any] | None = None, fail: str | None = None) -> str:
    """A Responses API stream: created → output_text.delta* → completed (or error)."""
    events: list[dict[str, Any]] = [
        {
            "type": "response.created",
            "sequence_number": 0,
            "response": {"id": "resp_1", "object": "response", "status": "in_progress", "output": []},
        }
    ]
    for i, d in enumerate(deltas):
        events.append(
            {
                "type": "response.output_text.delta",
                "sequence_number": i + 1,
                "item_id": "msg_1",
                "output_index": 0,
                "content_index": 0,
                "delta": d,
                "logprobs": [],
            }
        )
    if fail:
        events.append(
            {"type": "error", "sequence_number": 99, "code": fail, "message": "failed", "param": None}
        )
    else:
        events.append(
            {
                "type": "response.completed",
                "sequence_number": len(deltas) + 1,
                "response": {
                    "id": "resp_1",
                    "object": "response",
                    "status": "completed",
                    "output": [],
                    "usage": usage
                    or {
                        "input_tokens": 1200,
                        "output_tokens": 40,
                        "total_tokens": 1240,
                        "input_tokens_details": {"cached_tokens": 1024},
                        "output_tokens_details": {"reasoning_tokens": 0},
                    },
                },
            }
        )
    return "".join(f"event: {e['type']}\ndata: {json.dumps(e)}\n\n" for e in events)


def responses_json(text: str) -> dict[str, Any]:
    return {
        "id": "resp_2",
        "object": "response",
        "created_at": 0,
        "status": "completed",
        "model": "fast-deployment",
        "output": [
            {
                "type": "message",
                "id": "msg_2",
                "status": "completed",
                "role": "assistant",
                "content": [{"type": "output_text", "text": text, "annotations": []}],
            }
        ],
        "usage": {
            "input_tokens": 50,
            "output_tokens": 20,
            "total_tokens": 70,
            "input_tokens_details": {"cached_tokens": 0},
            "output_tokens_details": {"reasoning_tokens": 0},
        },
        "parallel_tool_calls": True,
        "tool_choice": "auto",
        "tools": [],
    }


def parse_sse(body: str) -> list[tuple[str, dict[str, Any]]]:
    out: list[tuple[str, dict[str, Any]]] = []
    for block in body.replace("\r\n", "\n").split("\n\n"):
        event = "message"
        data: list[str] = []
        for line in block.split("\n"):
            if line.startswith("event:"):
                event = line[6:].strip()
            elif line.startswith("data:"):
                data.append(line[5:].strip())
        if data:
            out.append((event, json.loads("\n".join(data))))
    return out
