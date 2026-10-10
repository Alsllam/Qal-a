# Qal'a architecture

This is the contract between the apps and services in this monorepo. When an endpoint, event or port changes, update this file in the same PR.

## 1. System map

```
                       ┌──────────────────────── clients ─────────────────────────┐
                       │  mobile/ (Flutter)           frontend/ (Angular, Nx)     │
                       │  play · learn · online ·     admin console: dashboard,   │
                       │  coach                       players, matches, content   │
                       └───────────────┬───────────────────────────┬──────────────┘
                                       │ HTTPS + WebSocket (SignalR)│
                               ┌───────▼───────────────────────────▼───────┐
                               │  Qala.Game.BFF.Host (YARP)  :5000         │  ← the only public entry
                               └──┬──────────┬───────────┬──────────┬──────┘
             /connect/** /account │  /players │ /matches  │ /hubs    │ /ai-api/**
                     ┌────────────▼┐  ┌──────▼─────┐ ┌───▼────────┐ ┌▼──────────────┐
                     │ Auth.Host   │  │ Players    │ │ Matches    │ │ ai-service    │
                     │ OpenIddict  │  │ .Host      │ │ .Host      │ │ (FastAPI)     │
                     │ :5001       │  │ :5102      │ │ :5101      │ │ :8000         │
                     └─────────────┘  └─────▲──────┘ └──┬─────────┘ └───────────────┘
                                            │ MatchFinishedEto (RabbitMQ / MassTransit)
                                            └───────────┘
```

| Part | Path | Stack | Role |
|---|---|---|---|
| Rules engine | `packages/game_core` | Dart | The game rules. Used by mobile, the prototype and the balance lab |
| AI players | `packages/game_ai` | Dart | Alpha-beta AI (on-device opponents and hints) |
| Rules (server) | `backend/Shared/Qala.Game.Rules` | C# | Port of `game_core`, kept identical by the shared test vectors |
| Backend | `backend/` | .NET 9 modular | Auth, players and ratings, online matches |
| AI coach | `ai-service/` | Python FastAPI + Azure OpenAI | Rules Q&A (RAG) and post-game review |
| Mobile app | `mobile/` | Flutter (clean architecture, BLoC, Flame board) | The game |
| Admin console | `frontend/` | Angular 20 + Nx 21 | Live balance dashboard, players, matches, content |
| Balance lab | `tools/balance` | Dart | Offline AI self-play reports |

## 2. Rules: one source of truth, two implementations

- `packages/game_core` (Dart) is the **reference** implementation.
- `backend/Shared/Qala.Game.Rules` (C#) exists because the server is **authoritative** in online play: it validates every move. Its tests replay `packages/game_core/test_vectors/rules_v<version>.json` (2,146 positions with legal moves, supply, the resulting position and the outcome, plus perft counts) and must match exactly.
- To change a rule: change `game_core`, run `dart run tool/export_vectors.dart`, update the C# port until its vector tests pass, and bump `RuleSet.version`.

## 3. Naming and ports

- .NET: `Qala.Framework.*` for the shared framework, `Qala.Game.{Module}.*` for modules and hosts.
- The rules version travels with every match (`rulesVersion`), so old games always replay correctly.

| Host | Port (dev) | Routes through the BFF |
|---|---|---|
| `Qala.Game.BFF.Host` | 5000 | — |
| `Qala.Game.Auth.Host` | 5001 | `/connect/**`, `/account/**`, `/.well-known/**` |
| `Qala.Game.Matches.Host` | 5101 | `/matches-api/**`, `/hubs/match` (WebSocket) |
| `Qala.Game.Players.Host` | 5102 | `/players-api/**` |
| `ai-service` | 8000 | `/ai-api/**` |
| `frontend` (dev server) | 4200 | — |

## 4. Auth

- OpenIddict, authorization code + PKCE for the mobile app and admin console. Refresh tokens are on.
- Scopes and audiences: `players-api`, `matches-api`, `ai-api`.
- Guest play: the app can play offline, and against the AI, without an account. Online play requires sign-in (email or phone OTP, plus Apple/Google later).
- Permissions follow `Permissions.{Area}.{Action}`, e.g. `Permissions.Matches.ViewMatch`, `Permissions.Players.ManagePlayer`, `Permissions.Content.ManageLessons`, `Permissions.Dashboard.ViewBalance`.

## 5. API contract (backend convention: reads are POST with a body)

### Players (`/players-api/players`)
| Endpoint | Body → Result | Permission |
|---|---|---|
| `POST me` | — → `PlayerProfileDto` (creates the profile on first call) | signed in |
| `PUT me` | `UpdateMyProfileDto { displayName, avatarId, locale }` | signed in |
| `POST list` | `FilterPlayerDto` → `PagedResultDto<PlayerListDto>` | `Permissions.Players.ViewPlayer` |
| `POST getbyid` | `{ id }` → `PlayerDto` | `Permissions.Players.ViewPlayer` |
| `POST leaderboard` | `{ skipCount, maxResultCount }` → paged `{ rank, displayName, rating }` | signed in |
| `POST activate` / `deactivate` | `{ id }` (ban / unban) | `Permissions.Players.ManagePlayer` |

Ratings use **Glicko-2**, starting at 1500 ± 350. They are updated by the `MatchFinishedEto` consumer.

### Matches (`/matches-api/matches`)
| Endpoint | Body → Result | Permission |
|---|---|---|
| `POST queue` | `{ timeControl }` → `{ ticketId }`; a match is pushed later through the hub | signed in |
| `DELETE queue` | `{ ticketId }` | signed in |
| `POST challenge` | `{ timeControl }` → `{ code }` (6 characters, share with a friend) | signed in |
| `POST challenge/accept` | `{ code }` → `MatchDto` | signed in |
| `POST getbyid` | `{ id }` → `MatchDto` (moves, clocks, status, rulesVersion) | a participant, or `ViewMatch` |
| `POST mine` | filter → paged `MatchListDto` | signed in |
| `POST list` | `FilterMatchDto` → paged `MatchListDto` | `Permissions.Matches.ViewMatch` |
| `POST stats` | `{ from, to, rulesVersion? }` → `BalanceStatsDto` (South score, end reasons, length, by day) | `Permissions.Dashboard.ViewBalance` |

`MatchDto`:

```json
{ "id": "…", "rulesVersion": "0.6", "status": "Active|Finished|Aborted",
  "south": { "playerId": "…", "displayName": "…", "rating": 1500 },
  "north": { … }, "position": "<notation incl. water>", "moves": ["c2-c3", …],
  "clocks": { "southMs": 240000, "northMs": 240000, "incrementMs": 2000 },
  "outcome": null | { "winner": "south|north|null", "reason": "waterVictory|…|timeout|resign|abandon" } }
```

### Real-time: SignalR hub `/hubs/match`
| Client → server | Server → client |
|---|---|
| `JoinMatch(matchId)` | `MatchState(MatchDto)` (on join and reconnect) |
| `MakeMove(matchId, move, ply)`: `ply` must equal the server ply (an idempotent retry is ignored) | `MoveMade({ matchId, move, ply, position, clocks, outcome? })` |
| `Resign(matchId)`, `OfferRematch(matchId)` | `MatchFound({ matchId, side })`, `MatchEnded({ matchId, outcome })`, `MoveRejected({ reason })` |

The server validates every move with `Qala.Game.Rules`, runs the clocks (default time control **4 min + 2 s**, provisional), and ends the game on timeout, resignation, or 60 s of disconnection.

### Events (MassTransit / RabbitMQ)
| Event | Publisher | Consumers |
|---|---|---|
| `MatchFinishedEto { matchId, rulesVersion, southPlayerId, northPlayerId, winner, reason, plies, durationMs, finishedAt }` | Matches | Players (ratings), and analytics |
| `PlayerBannedEto { playerId }` | Players | Matches (aborts the player's active games) |

## 6. AI coach (`/ai-api/**`)

| Endpoint | Purpose |
|---|---|
| `POST /ai-api/chat` (SSE) | Rules and strategy Q&A, grounded in `docs/rules.md`, the lessons and the strategy notes (RAG), with citations |
| `POST /ai-api/coach/review` (SSE) | Post-game review: `{ record, keyMoments[] , locale, level }` → short explanations of 3 key moments |
| `POST /ai-api/search` | Retrieval only (in-app rules search) |

**Engine analysis stays in Dart.** The app finds the key moments on the device (evaluation swings from `game_ai`) and sends them as `keyMoments: [{ ply, move, bestMove, evalBefore, evalAfter, tags: ["cut_supply", "lost_well", …] }]`. The AI service turns those facts into teaching language, grounded in the rules and lessons. It never invents moves: it only explains what the engine found. No model key is ever on the device.

## 7. Telemetry → live balance dashboard

Every finished online match becomes a `MatchFinishedEto`. `POST /matches-api/matches/stats` aggregates the same metrics as the balance lab (South score, draws, length, end reasons). The admin console shows them per rules version, so the human results can be compared with the AI baseline in `docs/balance-log.md`.
