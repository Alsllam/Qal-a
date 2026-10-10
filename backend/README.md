# Qal'a backend (.NET 9)

The server side of Qal'a: accounts (OpenIddict), player profiles and Glicko-2 ratings, and server-authoritative online matches. The API and event contract is [`docs/architecture.md`](../docs/architecture.md). The game rules are a C# port of `packages/game_core`, kept identical by the shared test vectors.

## Layout

```
backend/
├── Qala.Game.sln
├── Directory.Build.props / Directory.Packages.props   # net9.0, Nullable, central package versions
├── docker-compose.yml + .env.example                 # dev infra: SQL Server, RabbitMQ, Redis
├── .config/dotnet-tools.json                         # dotnet-ef 9.0.20 (local tool)
├── Shared/
│   ├── Qala.Game.Rules/                    # pure C# port of game_core rules v0.6 (no dependencies)
│   ├── Qala.Framework.Domain/              # entities, repositories, exceptions, events (ETOs), ICurrentUser, Identity users
│   ├── Qala.Framework.Application/         # ApplicationService, dynamic controllers, [HasPermission], error middleware, JSON localization
│   ├── Qala.Framework.EntityFrameworkCore/ # Repository<T>, UnitOfWork (auditing, soft delete), MassTransit, Identity/OpenIddict DbContext
│   └── Qala.Game.DbMigrator/               # applies all migrations, runs the seeders
├── Modules/
│   ├── Matches/  Qala.Game.Matches.{Domain,Application,EntityFrameworkCore,Tests}   # schema "matches"
│   └── Players/  Qala.Game.Players.{Domain,Application,EntityFrameworkCore,Tests}   # schema "players"
├── Hosts/
│   ├── Qala.Game.BFF.Host/      :5000  YARP gateway, the only public entry point
│   ├── Qala.Game.Auth.Host/     :5001  OpenIddict server + minimal login
│   ├── Qala.Game.Matches.Host/  :5101  matches API + SignalR hub /hubs/match
│   └── Qala.Game.Players.Host/  :5102  players API
└── tests/
    ├── Qala.Game.Rules.Tests/   # rules parity against packages/game_core/test_vectors
    └── Qala.Framework.Tests/    # error shape, ar/en resources, Matches host composition
```

Dependencies point one way: `Domain` ← `EntityFrameworkCore` ← `Application` ← `Host`. Modules never reference each other. They talk through MassTransit events (`Qala.Framework.Domain/Events`).

### Module map

| Module | Owns | Publishes | Consumes |
|---|---|---|---|
| Matches | `Match` (players, rules version, moves, clocks, outcome), `PlayerRating` read model, matchmaking queue, `MatchHub` | `MatchFinishedEto` | `PlayerRatingChangedEto` (read model), `PlayerBannedEto` (aborts open games) |
| Players | `Player` (profile, Glicko-2 rating, record), `RatedMatch` (idempotency) | `PlayerRatingChangedEto`, `PlayerBannedEto` | `MatchFinishedEto` (ratings) |
| Auth (framework) | `AppUser`, `AppRole` (permissions as role claims), OpenIddict tables, schema `auth` | — | — |

Player ids in matches and events are the auth user ids (the `sub` claim). `Player.Id` is the Players module's own key, used by the admin endpoints (`getbyid`, `activate`, `deactivate`).

## Ports and routes (dev)

| Through the BFF (:5000) | Goes to | Notes |
|---|---|---|
| `/connect/**`, `/account/**`, `/.well-known/**` | Auth.Host :5001 | Original `Host` header kept, so discovery advertises the BFF address |
| `/players-api/**` | Players.Host :5102 | `/players-api` prefix removed (`/players-api/players/me` → `/players/me`) |
| `/matches-api/**` | Matches.Host :5101 | prefix removed |
| `/hubs/match` | Matches.Host :5101 | WebSockets; cookie session affinity |
| `/ai-api/**` | ai-service :8000 | path kept |

Every host has `GET /health`. Swagger UI (`/swagger`) is on only in Development.

## Running locally

Requires the .NET 9 SDK and Docker.

```sh
cd backend
cp .env.example .env            # set QALA_SQL_PASSWORD and QALA_RABBITMQ_PASSWORD
docker compose up -d            # SQL Server :1433, RabbitMQ :5672 (UI :15672), Redis :6379

# Secrets are never in appsettings. Use environment variables (or dotnet user-secrets):
export SQL="Server=localhost,1433;Database=QalaGame;User Id=sa;Password=<QALA_SQL_PASSWORD>;TrustServerCertificate=True"
export ConnectionStrings__Auth="$SQL" ConnectionStrings__Matches="$SQL" ConnectionStrings__Players="$SQL"
export MessageBroker__Username=qala MessageBroker__Password=<QALA_RABBITMQ_PASSWORD>
export Seed__AdminEmail=admin@example.com Seed__AdminPassword=<a strong password>   # optional first admin

dotnet run --project Shared/Qala.Game.DbMigrator      # migrations + permissions + OpenIddict clients/scopes

dotnet run --project Hosts/Qala.Game.Auth.Host         # :5001
dotnet run --project Hosts/Qala.Game.Players.Host      # :5102
dotnet run --project Hosts/Qala.Game.Matches.Host      # :5101
dotnet run --project Hosts/Qala.Game.BFF.Host          # :5000
```

The three DbContexts share one database, each in its own schema (`auth`, `matches`, `players`) with its own migrations history table. Hosts never migrate at startup.

Without RabbitMQ, set `MessageBroker__Transport=InMemory` on a host. Events then stay inside that process, so ratings are not updated across hosts; use it only to poke at one host.

### Signing in (dev)

1. Register: `POST http://localhost:5000/account/register` with `{ "email", "password", "displayName" }`.
2. Run the authorization code + PKCE flow with client `qala-mobile` (redirect `qala://auth/callback`) or `qala-admin` (redirect `http://localhost:4200/auth/callback`) and scopes `openid profile offline_access players-api matches-api`. The login page is `/account/login` (Arabic or English from `Accept-Language`).
3. Call the APIs with the access token. SignalR clients pass it as `?access_token=` on `/hubs/match`.

Admin permissions (`Permissions.Matches.ViewMatch`, `Permissions.Dashboard.ViewBalance`, `Permissions.Players.ViewPlayer`, `Permissions.Players.ManagePlayer`) belong to the `admin` role, which the DbMigrator seeds. They are copied into the access token as `permission` claims.

## Build and test

```sh
cd backend
dotnet build Qala.Game.sln        # zero warnings
dotnet test Qala.Game.sln
```

No SQL Server, RabbitMQ or Redis is needed for the tests: they use EF Core InMemory, MassTransit's in-memory test harness and a fake clock.

- **Rules parity** (`tests/Qala.Game.Rules.Tests`). It loads `packages/game_core/test_vectors/rules_v0.6.json` (it finds the repo root by walking up from the test binaries). For each of the 2,146 positions it checks the round-trip notation, the sorted legal moves, the supplied squares of each side, the position after the recorded move and the outcome. It also replays the 40 recorded games from the opening and checks perft depths 1 to 3 (14 / 196 / 3,302). All must match the Dart engine exactly. When a rule changes, regenerate the vectors with `dart run tool/export_vectors.dart` and update the port until this suite passes.
- **Matches**: challenge create/accept, matchmaking pairing, legal/illegal moves, turn order, idempotent retries and stale plies, a full 22-ply game ending in `amirCaptured` that publishes `MatchFinishedEto`, resignation, timeouts (sweeper and late move), balance stats, ban consumer, permission attributes, soft delete and audit fields.
- **Players**: Glicko-2 against Glickman's worked example, the profile on first call, validation, list/leaderboard, ban with `PlayerBannedEto`, and `MatchFinishedConsumer` on the MassTransit harness (including a duplicate delivery).
- **Framework**: the error shape for every exception type, Arabic messages, ar/en key parity of every `Resources` folder, and the Matches host booted with `WebApplicationFactory` (health, 401s, dynamic controller routes).

If ICU is missing on a Linux machine (`Couldn't find a valid ICU package`), install `libicu`, or set **both** `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` and `DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=false`. Invariant mode alone refuses to create the `ar` culture. The whole suite passes with both flags set, because the JSON localizer reads the language from the culture name. CI and this repo's dev container have ICU, so neither flag is needed there.

### Migrations

They were generated with the local `dotnet-ef` tool (the DbMigrator is the startup project; each EF project has a design-time factory):

```sh
dotnet tool restore
dotnet ef migrations add <Name> -p Modules/Matches/Qala.Game.Matches.EntityFrameworkCore -s Shared/Qala.Game.DbMigrator -c MatchesDbContext -o Migrations
dotnet ef migrations add <Name> -p Modules/Players/Qala.Game.Players.EntityFrameworkCore -s Shared/Qala.Game.DbMigrator -c PlayersDbContext -o Migrations
dotnet ef migrations add <Name> -p Shared/Qala.Framework.EntityFrameworkCore -s Shared/Qala.Game.DbMigrator -c QalaIdentityDbContext -o Identity/Migrations
```

CI fails if a model change has no migration (`dotnet ef migrations has-pending-model-changes`).

## Online play: how a move is handled

1. The client calls `MakeMove(matchId, move, ply)` on `/hubs/match`.
2. `MatchPlayService` loads the match, rebuilds the `GameState` from the stored notation for the match's `rulesVersion`, and checks the following:
   - the caller is a player and it is their turn;
   - `ply` equals the server ply (resending a move already played at its ply returns `Duplicate` and the current state; any other mismatch is `StalePly`);
   - the mover's clock has not run out (a late move loses on time);
   - the move is legal.
3. The match is saved with a concurrency stamp, so two devices racing get one `Conflict`. `MoveMade` then goes to the match group.
4. If the game ended (rules outcome, resignation, timeout), `MatchEnded` is pushed and `MatchFinishedEto` is published. Players updates both ratings once per match id.

The clocks default to 4 min + 2 s, and the time control is set per match (`"4+2"`). A background sweeper ends games whose side to move has run out of time.

## Decisions that differ from the house template

- **No AutoMapper.** AutoMapper below 15.1 has a high-severity advisory (GHSA-rvv3-g6hj-g44x, which breaks the zero-warning build through NuGet audit), and 15.x needs a commercial licence. Each module has small explicit `*Mappings.cs` extension methods instead.
- **MassTransit 8.5.x**, the last Apache-2.0 line (v9 is commercial).
- **OpenIddict 6.4**. Access tokens are signed but not encrypted, so the APIs validate them locally from the published keys.
- **Permissions as token claims**, checked by `PermissionHandler`. The distributed-cache lookup from the template is a TODO (see below).
- **`ActivableAppService`** checks its permission inside the method (`ActivationPermission`), because route attributes cannot be parameterized per module.

## TODO (not done in this MVP)

- **Jobs host** (Hangfire): not created. Nothing needs scheduled jobs yet. Clock timeouts run in a `BackgroundService` inside Matches.Host.
- **Auth**: phone OTP, Apple and Google sign-in, email confirmation, password reset, a branded login page, and production certificate rotation. Production needs `AuthServer__SigningCertificatePath` and `AuthServer__EncryptionCertificatePath` (PFX files) and their passwords.
- **Permissions cache**: read permissions from `IDistributedCache` (Redis) with a DB fallback, so revoked permissions apply before the token expires (30 min).
- **Disconnection rule**: end a game after 60 s without a connection (`Match.Abandon` exists; connection tracking does not).
- **Rematch**: `OfferRematch` only notifies the opponent; the rematch itself is not created.
- **Scale-out**: the matchmaking queue is in memory and SignalR has no backplane, so run one Matches.Host instance. Next steps are a Redis queue and backplane.
- **Transactional outbox**: events are published right after the save. A crash between the two loses the event. Next step is the MassTransit EF Core outbox.
- **Stats** are aggregated in memory over the selected range (at most 366 days). Move them to a SQL projection or a pre-aggregated table once there is real traffic.
- **Ratings in matches** come from a read model updated by `PlayerRatingChangedEto`. Players who have never opened their profile get 1500 until their first rated game.
- **No Refit clients** yet. No module calls another over HTTP.
- **Not run against real infrastructure here.** The SQL Server migrations, RabbitMQ transport and the full OIDC code flow were compiled and generated but only exercised against in-memory substitutes in this environment.
