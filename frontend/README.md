# Qal'a admin console (`frontend/`)

Angular 20 + Nx 21 workspace for the Qal'a admin console. It has a live balance dashboard, a players screen and a matches screen with a read-only board replay. Content management comes later. The API contract is in [`docs/architecture.md`](../docs/architecture.md) §4, §5 and §7. The brand comes from [`brand/`](../brand) (see [`docs/brand-kit.md`](../docs/brand-kit.md)).

## Run

Requires Node 22 (`.nvmrc`).

```sh
npm ci                                   # also runs tools/sync-brand.mjs (postinstall)
npx nx serve admin                       # http://localhost:4200, mock API on, Arabic RTL by default
npx nx run-many -t lint test build       # what CI runs (.github/workflows/frontend.yml)
npx nx build admin                       # production build → dist/apps/admin/browser (mocks never bundled)
npx nx build admin -c demo               # optimized build with the mock API → dist/apps/admin-demo/browser
node tools/screenshots.mjs               # headless screenshots of the demo build → docs/screenshots/
```

On the login page in mock mode, choose **Sign in as dev admin**. This is a fake login with every permission. Use the top bar to switch the language (العربية / English) and the theme (light / dark).

## Structure

| Project | Path | Tag | Contents |
|---|---|---|---|
| `admin` | `apps/admin` | `type:app` | Thin shell: `main.ts` (loads `assets/app-settings.json`, then bootstraps), `app.config.ts`, routes, login page, dev mock API (`src/mocks`), `public/i18n/{ar,en}.json` |
| `core` → `@qala-fe/Core` | `libs/core` | `type:core` | `RestService`, app-settings loader, OIDC (`provideQalaAuth`, `AuthService`, `authGuard`), `PermissionService` + `*appPermission` + `permissionGuard`, `Permissions` constants, `LocalizationService` + `enar` pipe, `ThemeModeService`, `RoutesService` (menu) |
| `theme-shared` → `@qala-fe/theme-shared` | `libs/theme-shared` | `type:ui` | Global theme (`src/styles`), layout shell (sidebar + top bar), page header, confirmation dialog, toaster, HTTP error → toast handler, list engine (`ListFilterService`, `<app-paged-table>`, `AbstractListComponent.confirmAndRun`), skeletons, empty state, icons, 403/404 page |
| `shared-charts` → `@qala-fe/Charts` | `libs/shared/charts` | `type:ui` | ECharts via ngx-echarts, with only the modules used and lazy loading. `brand-light` / `brand-dark` themes from `brand/dist/tokens.ts`, `<app-chart>` (RTL mirroring, loading/empty, reduced motion) and the option builders `barOption`, `lineOption`, `donutOption`, `kpiSparkline` |
| `shared-board` → `@qala-fe/Board` | `libs/shared/board` | `type:ui` | Notation parser (`parsePosition`, `parseMove`, `applyMove`, `replay`), `<app-board>` (SVG, 7×7, Wells c4/e4, Qal'a d1/d7) and `<app-board-replay>` |
| `players-proxy` → `@qala-fe/PlayersProxy` | `libs/shared/players-proxy` | `type:proxy` | `PlayersService` + DTOs (`/players-api/players/*`) |
| `matches-proxy` → `@qala-fe/MatchesProxy` | `libs/shared/matches-proxy` | `type:proxy` | `MatchesService` + DTOs (`/matches-api/matches/*`, including `stats`) |
| `{dashboard,players,matches}-config` | `libs/<feature>/config` | `type:config` | `provide<Feature>Config()`: menu entries with `requiredPolicy` |
| `{dashboard,players,matches}-ui-common` | `libs/<feature>/ui-common` | `type:feature` | Screens + routes (only the routes are exported) |
| `brand` | `tools/brand` | `type:tooling` | `nx run brand:sync`: runs `tools/sync-brand.mjs` |

Module boundaries are enforced by `@nx/enforce-module-boundaries` in `eslint.config.mjs`:

- an app may use any lib;
- a feature may use proxy, ui and core libs;
- config and proxy libs may use only core;
- ui libs may use only core;
- core uses nothing internal.

Features never import each other.

Permissions are checked in three places, always with the strings from `Permissions` in core:

- the menu (`requiredPolicy`);
- the route (`permissionGuard` + `data.requiredPolicy`);
- the element (`*appPermission`, e.g. the Ban/Unban buttons need `Permissions.Players.ManagePlayer`).

## Brand: single source

`tools/sync-brand.mjs` copies from `../brand` and is never edited by hand. Its outputs are git-ignored:

- `brand/logo/*.svg`, `brand/png/*` → `apps/admin/src/assets/brand/` (it also writes `site.webmanifest`);
- `brand/dist/_tokens.scss` → `libs/theme-shared/src/styles/generated/_brand-tokens.scss` (CSS variables for light and `[data-theme=dark]`);
- `brand/dist/tokens.ts` → `libs/shared/charts/src/lib/generated/brand-tokens.ts` (ECharts series and theme colours).

The script runs on `npm install` and, through Nx `dependsOn`, before every build, serve, lint and test. Components use only CSS variables. `libs/theme-shared/src/styles/_derived.scss` adds app-only tokens built from brand values: status-tag tints and dark-mode shadows. Fonts are self-hosted from `@fontsource/ibm-plex-sans-arabic` and `@fontsource/ibm-plex-sans` (weights 400–700, Arabic and Latin subsets).

## Runtime configuration and mocks

- `apps/admin/src/config/{development,production}/app-settings.json` is copied to `assets/app-settings.json` by the build configuration. Deployment replaces that file. It is public, so never put secrets in it.
- `oAuthConfig` uses the authorization code flow with PKCE (angular-oauth2-oidc) against the Auth host. The access token is sent only to the hosts listed in `apis`.
- **Mocks** apply only when `useMockApi: true` (the development settings) **and** the build allows them. `main.ts` then lazy-loads `src/mocks/mock-api.ts`, and `mockApiInterceptor` (in core) answers `players-api` and `matches-api` calls from memory, with 220–480 ms latency. Ban/unban changes the in-memory data.
  - The demo data is deterministic: 137 players, 240 matches and generated balance stats.
  - The match records are the 40 real self-play games from `packages/game_core/test_vectors/rules_v0.6.json`, extracted once into `src/mocks/sample-games.ts`, so every replay is a legal game.
- The **production** configuration swaps `load-mock-api.ts` for `load-mock-api.prod.ts` (which returns null), and `environment.prod.ts` sets `allowMockApi: false`. The mock code is not even in the production bundle, and the production `app-settings.json` has `useMockApi: false` (checked by a unit test).
- In mock mode the login page offers **Sign in as dev admin**. This sets a session flag and grants `ALL_PERMISSIONS`. It is never offered with the real backend.

## Tests

Jest, per project: `npx nx test <project>`. The suites cover:

- proxy URLs, verbs and bodies (`HttpTestingController`);
- the notation parser, the board and the replay;
- the permission service, directive and guard;
- `RestService` and the error reporter;
- the `enar` pipe and the RTL switch;
- the list engine (debounce, paging, removal);
- the players list hiding Ban/Unban without `ManagePlayer`;
- the chart option builders and RTL mirroring;
- the menu config;
- i18n key parity and "Arabic is not an English copy";
- mock-data consistency.

## TODO / known gaps

- **Backend contract details to confirm:**
  - the `BalanceStatsDto` shape (proposed in `libs/shared/matches-proxy/src/lib/models/match.model.ts`; `docs/architecture.md` lists only its content);
  - the name of the permission claim in the access token (`permission`/`permissions` is assumed, in `readPermissionClaims`);
  - the OIDC client id `Qala_Admin`.
- **Replay water points:** water is shown only on the final position, from `MatchDto.position`. Intermediate water needs the rules engine, or per-ply positions from the server.
- **Deviations from the house skill:**
  - No Bootstrap / ng-bootstrap / ngx-datatable yet. A small token-based CSS layer with logical properties and the `<app-paged-table>` wrapper cover this skeleton.
  - There is no `theme-layout-generator` and no dim theme (only light and dark).
  - There is no `shared/ui-common` (wizard and `mof-input-*`), because there are no forms yet. Add it with the content-management feature.
- Advanced filter side panel, filter chips, export, and saved sorting are not done. The list engine keeps per-screen filter state in session storage.
- The confirmation dialog focuses the confirm button and closes on Esc, but it does not fully trap focus.
- No Playwright E2E suite. `tools/screenshots.mjs` is a smoke run: it fails on console errors or horizontal overflow.
- No Hijri calendar or date pickers (no forms yet).
- Content management (`Permissions.Content.ManageLessons`) is not started.
