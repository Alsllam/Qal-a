"""Azure AI Content Safety Prompt Shields (jailbreak + indirect injection), optional.

Enabled with ``PROMPT_SHIELDS_ENABLED=true`` and ``CONTENT_SAFETY_ENDPOINT``. Qal'a's knowledge
base is written by the team, so it is off by default; turn it on when lessons come from outside.
"""

from __future__ import annotations

from collections.abc import Awaitable, Callable
from dataclasses import dataclass

import httpx

TokenProvider = Callable[[], Awaitable[str]]

API_VERSION = "2024-09-01"


@dataclass(frozen=True)
class ShieldResult:
    user_attack: bool
    document_attacks: list[bool]

    @property
    def any_attack(self) -> bool:
        return self.user_attack or any(self.document_attacks)


class PromptShields:
    def __init__(
        self,
        http: httpx.AsyncClient,
        endpoint: str,
        *,
        api_key: str | None = None,
        token_provider: TokenProvider | None = None,
    ) -> None:
        if not api_key and token_provider is None:
            raise ValueError("Prompt Shields needs an API key (local) or an Entra token provider")
        self._http = http
        self._url = f"{endpoint.rstrip('/')}/contentsafety/text:shieldPrompt"
        self._api_key = api_key
        self._token_provider = token_provider

    async def check(self, user_prompt: str, documents: list[str]) -> ShieldResult:
        headers: dict[str, str] = {}
        if self._api_key:
            headers["Ocp-Apim-Subscription-Key"] = self._api_key
        elif self._token_provider is not None:
            headers["Authorization"] = f"Bearer {await self._token_provider()}"
        response = await self._http.post(
            self._url,
            params={"api-version": API_VERSION},
            json={"userPrompt": user_prompt, "documents": documents},
            headers=headers,
            timeout=10.0,
        )
        response.raise_for_status()
        data = response.json()
        user = bool(data.get("userPromptAnalysis", {}).get("attackDetected", False))
        docs = [bool(d.get("attackDetected", False)) for d in data.get("documentsAnalysis", [])]
        return ShieldResult(user_attack=user, document_attacks=docs)
