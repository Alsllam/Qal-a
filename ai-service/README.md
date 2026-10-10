# Qal'a AI service (`ai-service/`)

The AI coach for Qal'a: rules Q&A grounded in `docs/rules.md` (RAG with citations) and the post-game
review that explains the key moments the on-device engine found. It is the **only** component that
talks to AI models. The mobile app and admin console reach it through the BFF at `/ai-api/**` with
the user's OpenIddict access token. No client ever holds a model key.

Contract: `docs/architecture.md` section 6. Voice: `docs/brand-kit.md` section 7.

## Architecture

```
mobile / admin ──▶ BFF (YARP) ──▶ ai-service (FastAPI, :8000)
                                   ├─ api/deps.py      JWT (JWKS, aud=ai-api) → CurrentUser, rate limit, permissions
                                   ├─ rag/answer.py    rewrite → retrieve (security filter) → context → stream
                                   ├─ coach/review.py  key moments → rules lookup → stream (move guard)
                                   ├─ llm/gateway.py   Azure OpenAI Responses + Embeddings, one retry policy
                                   ├─ rag/index/*      Azure AI Search schema + writer, in-memory BM25 index
                                   └─ SQL (SQLAlchemy 2 async + Alembic): conversations, messages,
                                      ingestion_jobs, usage
```

| Path (`src/ai_service/`) | What it does |
|---|---|
| `settings.py` | All configuration (pydantic-settings). `ModelRole` → deployment name via `DEPLOYMENTS__<ROLE>` |
| `llm/client.py` | `AsyncOpenAI` on the Azure **v1** endpoint (`{endpoint}/openai/v1/`). Entra ID token provider (refreshed per request); an API key only when `ENVIRONMENT=local/test` |
| `llm/gateway.py` | Streaming (`responses.create(stream=True)`), structured output (strict JSON schema from Pydantic), embeddings. Tenacity: 429/5xx retried with backoff honoring `Retry-After`; timeouts retried only for idempotent calls |
| `llm/prompts/*.vN.md` | Versioned prompts: `answer.v1`, `coach_review.v1`, `query_rewrite.v1`, `groundedness.v1` (eval grader). Changes go in a new version file |
| `api/errors.py` | Backend error shape `{ "error": { "code", "date", "messages", "source": "Ai" } }`, `ar`/`en` messages by `Accept-Language`, OpenAI exception mapping |
| `auth.py`, `api/deps.py` | JWKS cache (refetch on unknown `kid`), audience/issuer/expiry checks, `sub`/tenant/roles/permissions; per-user sliding-window rate limit (`ratelimit.py`) |
| `rag/ingestion/` | Arabic normalizer (search copy), heading-aware Markdown chunker, idempotent pipeline (doc id + SHA-256), repo docs loader |
| `rag/index/schema.py` | Azure AI Search index: filterable `tenant_id`/`acl_groups`/`document_id`/`doc_type`/`language`, `content_ar` (`ar.microsoft`), `content_en` (`en.microsoft`), normalized `content_search`, HNSW cosine vector, semantic config (title/content/heading_path) |
| `rag/retrieval/` | `security.py` (filter from the token only), `search.py` (hybrid + semantic + reranker threshold, dedupe, per-doc cap), `rewrite.py` (fast model, structured output), `context.py` (token budget, `<source id="S#">`, citation map) |
| `rag/glossary.py` | Arabic → English game vocabulary used to expand Arabic keyword queries (the rules are in English) |
| `safety/` | PII redaction in logs, source wrapping that cannot be broken out of, optional Azure Prompt Shields |
| `usage/meter.py` | Tokens (input / cached / output), latency and cost per tenant, user, feature and deployment |
| `coach/guard.py` | Removes any move in Qal'a notation that the engine did not report, even when it is split across deltas |

### Design decisions worth knowing

- **Errors before the first token become HTTP statuses.** Each SSE endpoint runs its generator up to
  the first event before responding (`sse.prime`), so a content-filter hit on the prompt is a real
  `400`, Azure throttling a `503 General:Errors:AiBusy`, a timeout a `504`. Failures after streaming
  starts arrive as an `error` event and the partial answer is stored with `incomplete: true`.
- **Security trimming always comes from the token.** Request models ignore unknown fields; the
  `SecurityScope` (tenant + `public` + the user's roles) is built in `deps.py`; extra filters can only
  narrow on whitelisted fields (`doc_type`, `language`).
- **No sources, no model call.** If retrieval finds nothing, the answer is a localized "not in the
  official rules" message, with no tokens spent.
- **`store=False`.** History lives in our SQL tables (last `HISTORY_TURNS` turns are sent).
- **Prompt caching order:** static instructions → sources → history → question. `cached_tokens` is metered.

## Endpoints

| Endpoint | Auth | Notes |
|---|---|---|
| `POST /ai-api/chat` (SSE) | signed in, rate limited | `{ conversationId?, message, locale, context?: { screen, entityId } }` |
| `POST /ai-api/coach/review` (SSE) | signed in, rate limited | see below |
| `POST /ai-api/search` | signed in, rate limited | `{ query, top?, docType? }` → `{ items: [{ id, documentId, title, headingPath, snippet, content, page, url, score }] }` |
| `GET /ai-api/conversations?skip&take`, `GET/DELETE /ai-api/conversations/{id}` | owner only | history (list, read, delete) |
| `POST /ai-api/ingest`, `GET /ai-api/ingest/{jobId}`, `DELETE /ai-api/ingest/documents/{documentId}` | `INGEST_PERMISSION` (default `Permissions.Content.ManageLessons`) | Markdown lessons from the admin console |
| `GET /ai-api/admin/usage?days=7` | `ADMIN_PERMISSION` (default `Permissions.Dashboard.ViewBalance`) | tokens and cost by feature/deployment |
| `POST /ai-api/admin/index` | `INGEST_PERMISSION` | create/update the Azure AI Search index |
| `POST /ai-api/admin/retention` | `ADMIN_PERMISSION` | delete conversations older than `RETENTION_DAYS` |
| `GET /health/live`, `GET /health/ready` | none | ready checks DB, search and Azure OpenAI |

### Chat SSE events

`meta { conversationId, language, promptVersion, sources }` → `delta { text }`* →
`citations { citations: [{ id: "S1", documentId, title, headingPath, page, url }] }` →
`done { messageId, incomplete, noAnswer, usage: { inputTokens, cachedTokens, outputTokens } | null }`.
An `error { code, message }` event may come before `citations` when the stream fails midway.

### How the mobile app uses `POST /ai-api/coach/review`

Engine analysis stays in Dart: after a game, the app runs `game_ai` over the record, finds the
biggest evaluation swings and sends them as facts. The service never searches for moves.

```json
{
  "record": { "rulesVersion": "0.6", "moves": ["c2-c3", "c6-c5", "d2-d3", "…"], "playerSide": "south",
              "outcome": { "winner": "north", "reason": "waterVictory" } },
  "keyMoments": [
    { "ply": 5, "move": "c3xc4", "bestMove": "d3-d4", "evalBefore": 0.1, "evalAfter": -1.4, "tags": ["cut_supply"] }
  ],
  "locale": "ar",
  "level": "beginner"
}
```

- `ply` is 1-based in `record.moves` (odd plies are South). Evaluations are from the mover's point of
  view, in `game_ai` units (≈1.0 = one Jundi); won positions (±100000) are clamped to ±50.
- Every `keyMoments[i].move` must equal `record.moves[ply-1]`, otherwise `400 Coach:Errors:MoveMismatch`.
  Moves must be in Qal'a notation (`c2-c3`, `c3xd3`, `d2*d4`); tags are snake_case.
- Up to 12 moments may be sent; the service explains the **3 biggest swings**, in game order.
- Known tags that steer the rules lookup: `cut_supply`, `lost_supply`, `lost_well`, `missed_well`,
  `water_race`, `missed_capture`, `hanging_piece`, `amir_danger`, `missed_win`, `qala_threat`,
  `rami_shot`, `faris_slide`, `ply_limit`. Unknown tags are passed to the model but not used for lookup.

Events: `meta { reviewId, locale, level, promptVersion, moments: [{ index, ply, move, bestMove, side }] }`,
then per moment `moment_start { index, ply }`, `delta { index, text }`*,
`moment_end { index, ply, incomplete, removedMoves, citations }`, and finally `done { reviewId, usage }`.
`removedMoves > 0` means the model wrote a move the engine did not report; it was replaced by `…`
before reaching the player. The app shows the text with an "AI-generated" label.

## Configuration

All settings are environment variables (see `.env.example`, names only). Nested values use `__`.

| Variable | Purpose |
|---|---|
| `ENVIRONMENT` | `local`, `test`, `dev`, `staging`, `production` (API keys are refused outside local/test) |
| `AZURE_OPENAI_ENDPOINT` | `https://<resource>.openai.azure.com` (the service appends `/openai/v1/`) |
| `AZURE_OPENAI_API_KEY` | local development only; deployed environments use managed identity |
| `DEPLOYMENTS__CHAT`, `__FAST`, `__REASONING`, `__EMBED` | deployment name per model role |
| `EMBED_DIMENSIONS` | must match the index (default 1536) |
| `SEARCH_BACKEND` | `azure` (default) or `memory` (local only: in-process BM25 over `MEMORY_SEED_PATHS`) |
| `SEARCH_ENDPOINT`, `SEARCH_INDEX_NAME`, `SEARCH_API_KEY` (local) | Azure AI Search |
| `RERANKER_THRESHOLD`, `MAX_CHUNKS_PER_DOCUMENT`, `SEARCH_TOP`, `SEARCH_VECTOR_K` | retrieval tuning |
| `JWT_JWKS_URL`, `JWT_ISSUER`, `JWT_AUDIENCE` (`ai-api`) | OpenIddict validation |
| `TENANT_CLAIM`, `ROLES_CLAIM`, `PERMISSIONS_CLAIM`, `DEFAULT_TENANT_ID` | claim names; tokens without a tenant use `DEFAULT_TENANT_ID` |
| `INGEST_PERMISSION`, `ADMIN_PERMISSION` | permission names checked by admin routes |
| `RATE_LIMIT_PER_MINUTE`, `CONTEXT_BUDGET_TOKENS`, `CHAT_MAX_OUTPUT_TOKENS`, `COACH_MAX_OUTPUT_TOKENS`, `HISTORY_TURNS`, `RETENTION_DAYS` | limits and budgets |
| `ANSWER_PROMPT_VERSION`, `COACH_PROMPT_VERSION`, `REWRITE_PROMPT_VERSION` | which prompt file is live |
| `TOKENIZER_MODE` | `auto` (tiktoken, falls back to an estimate), `tiktoken`, `estimate` |
| `PROMPT_SHIELDS_ENABLED`, `CONTENT_SAFETY_ENDPOINT` | optional Prompt Shields |
| `DATABASE_URL` | SQLAlchemy async URL (`sqlite+aiosqlite:///…` locally) |
| `PRICES` | JSON, USD per 1M tokens per deployment: `{"<deployment>": {"input": 0, "cached_input": 0, "output": 0}}` |

OpenIddict must issue **signed, unencrypted** access tokens for the `ai-api` resource
(`DisableAccessTokenEncryption`), otherwise this service cannot read them.

## Run locally

```sh
cd ai-service
uv sync
cp .env.example .env              # fill in endpoint + deployments (+ a local key) and JWKS URL
uv run alembic upgrade head       # creates the local SQLite tables
uv run uvicorn ai_service.main:app --factory --reload --port 8000
```

Without Azure AI Search, run with `SEARCH_BACKEND=memory MEMORY_SEED_PATHS='["../docs/rules.md"]'`
(keyword search only). Chat and coach review always need Azure OpenAI, and every `/ai-api/**`
call needs a token from the Auth host (`JWT_JWKS_URL`). `/ai-api/docs` serves OpenAPI in local/dev.

Index the repo docs:

```sh
uv run python scripts/ingest_repo_docs.py --dry-run --show-text   # chunks only, no Azure
uv run python scripts/ingest_repo_docs.py --create-index          # index schema + embed + upsert
```

## Quality checks (what CI runs, `.github/workflows/ai-service.yml`)

```sh
uv run ruff check .
uv run ruff format --check .
uv run mypy --strict src scripts evals tests
uv run pytest -q
TOKENIZER_MODE=estimate uv run python evals/run_eval.py --retriever memory   # offline retrieval eval
```

Tests never touch the network: Azure OpenAI is mocked at the HTTP level with `respx` (real SDK,
real Responses SSE frames), search uses the in-memory index, JWTs are signed with an RSA key
generated in the test session, the database is SQLite.

### Evaluation

`evals/datasets/qala_rules_{en,ar}.jsonl` hold questions written from `docs/rules.md` with the
expected section headings, plus unanswerable questions. `evals/run_eval.py` computes recall@1/3/5
and MRR offline against the in-memory BM25 retriever (Arabic queries are expanded with
`rag/glossary.py`), and `--generation` adds groundedness (graded by the `reasoning` deployment),
citation rate, refusal accuracy and language match. `evals/thresholds.yaml` holds the minimums.

The corpus is one 270-line file (8 chunks), so recall@5 is close to trivially high today; watch
recall@1 and MRR, and grow the datasets toward 50+ questions per language as lessons are added.

## What needs real Azure resources

- **Azure OpenAI** with deployments for `chat`, `fast`, `embed` (and `reasoning` for the eval grader),
  plus the *Cognitive Services OpenAI User* role for the service's managed identity. Not exercised
  here: the Entra token flow against the real resource, real streaming payloads, content-filter
  annotations and `cached_tokens` values.
- **Azure AI Search** (semantic ranker enabled) with *Search Index Data Contributor/Reader*:
  create the index (`POST /ai-api/admin/index` or `--create-index`), ingest, then run
  `run_eval.py --retriever azure [--generation]` and calibrate `RERANKER_THRESHOLD`.
- **Azure SQL / SQL Server**: add an async driver (e.g. `aioodbc` + ODBC Driver 18 in the image) and
  set `DATABASE_URL`; the migrations use portable types.
- **Azure AI Content Safety** if Prompt Shields are enabled.

## Not built yet

- PDF/DOCX ingestion (Document Intelligence, Blob Storage), the `arq` worker and the RabbitMQ
  `DocumentUploaded`/`DocumentDeleted` consumer. Ingestion runs as an in-process background task.
- Tool calling, voice and vision endpoints from the platform skill (not in the Qal'a contract).
- OpenTelemetry export (logs are structured JSON with PII redaction).
- A shared (Redis) rate limiter: the current limiter is per replica.
- A smoke test against real Azure (the `azure` pytest marker is reserved for it).
