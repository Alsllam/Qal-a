# Qal'a (قلعة): Game Design Document

**Version 0.1 (2026-10-10).** Based on rules v0.6 (`docs/rules.md`), the balance lab (`docs/balance-log.md`) and the prototype (`prototype/`).

> ⚠️ **Written before the first human playtest round.** Every item marked **〔P〕** is *provisional*: it is a decision we expect the playtest (`docs/playtest-guide.md`) to confirm or change. Section 13 lists them all with the playtest signal that decides each one. Update this document after the playtest round, and remove each mark only when its question is answered.

---

## 1. Vision

**A strategy game you learn in five minutes and keep discovering for months, rooted in the desert fortresses and wells of Arabia.**

Two fortresses fight over two wells. An army can only attack while it is linked to water. Players win by capturing the enemy Amir, taking the enemy fortress, or gathering ten water points. There is no luck: every game is decided by the players.

### Pillars
1. **Five-minute learning.** Four pieces and one twist (water). The first game is playable after a 60-second intro.
2. **Months of depth.** Tactics (shots, captures) on top of structure (supply lines, wells), with an AI coach that explains *why*.
3. **Pocket-sized games.** 5–10 minutes on a phone, so a game fits a coffee break.
4. **Fair by design.** No dice and no hidden information. Balance is measured by the balance lab and by live telemetry.
5. **Heritage, not folklore.** Arabic-first, built in Arabic, with a calm, warm aesthetic of desert nights, wells and fortresses.

### Audience
- **Primary:** Arabic-speaking mobile players, 16–40, who enjoy chess, backgammon, carrom or Ludo-style social games but want something quicker and new.
- **Secondary:** strategy-board-game fans worldwide (English), and families or schools (learning path, safe social features).

## 2. The game in one screen

- 7×7 board, 8 pieces per side: Amir (leader), 4 Jundi, 2 Faris, 1 Rami.
- **Supply:** a piece can capture only if a chain of touching friendly pieces links it to your Qal'a or your Amir.
- **Wells:** each Well held by a supplied piece (not the Amir) earns +1 water point at the start of your turn.
- **Win:** capture the Amir, put a supplied piece on the enemy Qal'a, or reach 10 water points. After 60 plies, the player with more water wins.
- Full rules: `docs/rules.md`. Every game records its rules version, so old games always replay correctly.

## 3. First-time experience (the first 5 minutes)

| Time | What happens | Goal |
|---|---|---|
| 0:00 | Splash: the tower rises and the drop falls into the gate (800 ms). Language: Arabic or English (defaults to the device locale) | Brand and identity |
| 0:10 | **Intro, 60 seconds, interactive:** "This is your Amir. Protect him." → move a Jundi → "Pieces that touch your fortress or your Amir have water. Only they can attack." → make a first capture | One rule per tap, never a wall of text |
| 1:10 | **First game against the Shepherd** (the easiest AI), with **coach tips** on: the coach points out the first Well, the first cut-off piece, and the first chance to capture | Learn by playing |
| ~7:00 | Game ends (the Shepherd is tuned so that most new players win the first game 〔P〕). A short celebration, then: "Want to learn the tricks?" → learning path, or "Again?" | First win, and two clear next steps |

Sign-in is **never** required before the first game. It is only asked for online play and cloud saves.

## 4. Learning path (التعلّم)

Short interactive lessons (1–3 minutes each), grouped into **seven chapters**. Each lesson follows the same rhythm: *show → try → check* with a board puzzle.

| # | Chapter | Lessons (examples) | Unlocks |
|---|---|---|---|
| 1 | **The Fortress** (القلعة) | Amir · Jundi · Faris · Rami's step and shot · How to win | Play the AI ladder |
| 2 | **Water** (الماء) | What supply is · Chains (diagonals count) · Cutting a line · Reconnecting · The Amir as water | Puzzle type: "Cut the line" |
| 3 | **Wells** (الآبار) | Holding a Well · Why a cut-off holder earns nothing · Racing to 10 · Defending a Well for one turn | Puzzle type: "Hold the Well" |
| 4 | **Three ways to win** | Amir hunt · Taking the Qal'a · Water victory · The 60-ply limit | Daily puzzle |
| 5 | **Tactics** (التكتيك) | Rami shot lines · Faris raids · Double threats · Shooting through the gap · Sacrifices that cut supply | Online play suggested |
| 6 | **Strategy** (الاستراتيجية) | Opening: Jundi to the Well 〔P〕 · Developing the Rami · Amir safety vs Amir as water carrier · Tempo | Strategy notes in the coach |
| 7 | **Endgames** (النهايات) | Water races · Winning with few pieces · Holding the draw… then breaking it | Mastery badge |

- **Puzzles** are generated offline: the balance lab is extended with a puzzle miner. It searches AI self-play games for positions with a **unique** winning or saving move found by deep search, then a human curates them. They come in three difficulty bands, with puzzle ratings (Glicko-2, like players).
- **Daily puzzle:** one per day, the same for everyone, with a gentle streak and no punishment for missing a day.
- **Stars:** 1–3 per lesson (solved first try / with a hint / with the answer shown). Stars unlock cosmetics, never power.
- **Content format:** lessons are data (`lessons/*.json`: positions in notation, scripted steps, coach lines in ar/en), so new lessons ship without an app update (from the content admin in the console, later). Lesson text is **written by people**, not generated by the LLM.
- **Coach lines in lessons are deterministic**, from pre-written templates. The LLM coach is only used for open questions and post-game reviews (section 6).

## 5. AI opponents (the ladder)

Eight named opponents, from heritage characters, all powered by `packages/game_ai` **on the device** (offline, free to run):

| Level | Name | Engine setting 〔P〕 | Target win rate for a player at that stage 〔P〕 |
|---|---|---|---|
| 1 | Shepherd (الراعي) | depth 1, high noise (1.5) | 80% for a brand-new player |
| 2 | Camel Driver (الحادي) | depth 1, noise 0.6 | 65% after chapter 1 |
| 3 | Merchant (التاجر) | depth 2, noise 0.4 | 55% after chapter 2 |
| 4 | Guard (الحارس) | depth 2, noise 0.15 | 50% after chapter 3 |
| 5 | Commander (القائد) | depth 3, noise 0.2 | 45% after chapter 4 |
| 6 | Amir (الأمير) | depth 3, noise 0.05 | 40% after chapter 5 |
| 7 | Vizier (الوزير) | depth 4, noise 0.05 | strong club player |
| 8 | Sultan (السلطان) | depth 5 with move ordering and a transposition table (to build), no noise | the boss |

- **Calibration:** each level is rated by self-play against the others in the balance lab (an Elo ladder). After launch, the win rates from telemetry adjust the noise and depth. Levels must feel **different, not just stronger**: each persona gets a style bias in the evaluation (the Merchant loves Wells, the Commander loves captures, the Vizier loves cutting lines) 〔P〕.
- **Beating a level** unlocks the next and a badge. Re-challenge is always free.
- **Casual help:** take-backs and up to 3 hints per game in "Learn" games, none in "Ladder" games.
- **Performance:** each AI move runs in a background isolate. Depth 4–5 need a stronger search (iterative deepening, transposition table, killer moves) before shipping levels 7–8. That is engineering work in `game_ai`.

## 6. AI coach (المدرّب)

Three jobs, each with clear limits:

1. **Hints during a game** (on the device, no network). The engine suggests a move and explains it with templates: *"This move cuts their Faris from water."* Tags come from the engine (supply cut, Well taken, Amir threatened, shot available).
2. **Post-game review** (online, `POST /ai-api/coach/review`). The app picks the **three key moments** (the largest evaluation swings) and sends the facts (move, best move, evaluation before and after, tags). The AI service turns them into short, warm explanations in Arabic or English, grounded in the rules and lessons. **The model never invents moves**: it only explains what the engine found. Without a network, the app falls back to template explanations.
3. **Ask the coach** (online chat, `POST /ai-api/chat`). Questions about rules and strategy, answered from `docs/rules.md`, the lessons and the strategy notes, with citations. Off-topic questions are politely declined.

Every coach answer is labelled "AI-generated", can be reported in one tap, and uses the brand voice (`docs/brand-kit.md` §7). There are cost limits per user (section 12), and no model key on the device (`docs/architecture.md` §6).

## 7. Progression

Progression rewards **learning and playing**, never paying for power.

| System | How it grows | What it gives |
|---|---|---|
| **Stars** (learning path) | 1–3 per lesson/puzzle | Unlock chapters and cosmetics |
| **AI ladder** | Beat each named opponent | Badges, and the next opponent |
| **Rating** (online, Glicko-2) | Ranked online games; provisional for the first 10 games | Matchmaking and leaderboards. Shown as a number plus a tier name: Sand, Stone, Bronze, Silver, Gold, Fortress 〔P〕 |
| **Puzzle rating** | Rated puzzles | Puzzle difficulty matched to the player |
| **Achievements** | "First cut", "Ten waters", "Amir hunter", "Win with the Rami"… | Profile badges |
| **Cosmetics** | Stars, achievements, seasons | Board themes (Desert Night, Oasis, Sea Trade, Old Map), piece sets, Qal'a skins, emotes |

**Seasons** (later): 8 weeks, a soft rating reset, season rewards (cosmetic only), and a themed event (e.g. the *Mawsim* sea variant as a limited-time mode).

**Monetization 〔P〕:** none at MVP. Later options in priority order: (1) cosmetic packs; (2) "Coach Plus", which adds unlimited post-game reviews and deeper analysis (free tier: 3 reviews per day); (3) no ads during games, ever. Rewarded ads, if any, only for an optional extra puzzle. Revisit after the beta, using the cost per coach review from usage metering.

## 8. Online play

| Feature | MVP? | Notes |
|---|---|---|
| **Quick match** (rated) | ✅ | Matchmaking by rating window, widening over 20 s. Time control **4 min + 2 s** 〔P〕 |
| **Friend challenge** (code or link) | ✅ | Unrated or rated, choice of time control |
| **Reconnect** | ✅ | The game state is restored from the server. Your clock keeps running while you are away, and the game is forfeited after 60 s offline 〔P〕 |
| **Rematch, resign** | ✅ | — |
| **Communication** | ✅ preset only | Six preset messages and emotes ("Good game", "Well played", "Oops"). No free chat (safety for younger players) |
| **Spectate, replays, share a game** | Replay ✅; spectating later | A replay of any finished game, shareable as a link with the notation |
| **Daily / correspondence games** | Later | 24 hours per move |
| **Tournaments, clubs** | Later | Arena-style weekend tournaments |
| **Team 2v2 / 4-player** | Later (research) | Plus-shaped board, shared water between partners (see `docs/concepts.md`) |

**Server-authoritative.** The C# rules port validates every move (`docs/architecture.md` §2). Clocks run on the server.

**Fair play.** Detecting engine use (comparing a player's moves with the engine's top choices, plus timing patterns) comes after the beta. Repeated abandoning causes a temporary queue cooldown.

**Telemetry.** Every finished match feeds the live balance dashboard (South score, end reasons, length) per rules version, compared with the AI baseline. If live South scores stay above 55% over 1,000+ rated games, apply rules v0.7 or the pie rule (`docs/balance-log.md`).

## 9. Look, feel and accessibility

- **Brand:** `docs/brand-kit.md`: indigo night, saffron, water blue and warm sand, with IBM Plex Arabic and Latin fonts.
- **Board:** the same readable language as the prototype. Supplied pieces glow, cut-off pieces are faded with a "no water" mark, and legal targets are dots / red rings / crosshairs. A water ripple shows supply reconnecting. A drop falls into the water bar when water is gained.
- **Colour independence:** pieces differ by **shape and letter**, not only colour. Supply is shown by both glow and the dry mark. The theme is tested with colour-blindness simulation.
- **Arabic-first:** RTL interface, MSA copy, and optional Arabic-Indic digits. The board itself keeps files a–g left to right in both languages, so notation stays universal.
- **Sound and haptics:** soft wood-on-board clicks, a water "drip" when water is gained, a light haptic on capture. Everything can be switched off.
- **Accessibility:** 48×48 tap targets, text scaling up to 1.3×, reduced motion honoured, and a screen reader mode that announces moves in words ("Faris from b1 to b4, captures Jundi").

## 10. Platforms and technology

- **Mobile app (MVP):** Flutter for Android and iOS (`mobile/`). Clean architecture, BLoC, a Flame board, and offline-first: learning, the AI and pass-and-play work fully offline.
- **Backend:** .NET 9 modular (`backend/`): Auth (OpenIddict), Players (profiles, Glicko-2), Matches (matchmaking, SignalR, server-validated rules).
- **AI coach:** Python FastAPI on Azure OpenAI with Azure AI Search (`ai-service/`).
- **Admin console:** Angular 20 + Nx (`frontend/`): balance dashboard, players, matches, content (lessons and puzzles) later.
- Details: `docs/architecture.md`.

## 11. MVP scope

### Milestones

| Milestone | Content | Exit criteria |
|---|---|---|
| **M0: Playtest round 1** | Prototype + guide (done) | Section 13 questions answered; rules frozen as v1.0 (or v0.7) |
| **M1: Offline MVP** (closed test) | Mobile: intro, chapters 1–4 (≈ 18 lessons), 50 curated puzzles + daily puzzle, AI ladder levels 1–6, pass-and-play, on-device hints, local stats, ar/en, brand, sounds | Tutorial completion ≥ 70%; first-game win rate vs the Shepherd 70–90%; crash-free sessions ≥ 99.5% |
| **M2: Online beta** | Accounts, quick match + friend challenge, ratings, leaderboards, replays, admin console (dashboard, players, matches), telemetry | Matchmaking time p50 < 20 s at beta size; live South score within 50 ± 5%; D7 retention ≥ 15% |
| **M3: Coach** | Post-game review + "Ask the coach" (ar/en), with evals passing the thresholds | Groundedness ≥ 0.9, refusal accuracy ≥ 0.9; ≥ 30% of players open at least one review |
| **Public launch** | Chapters 5–7, ladder levels 7–8, seasons, cosmetics | Store rating ≥ 4.5 in the beta, D1 ≥ 35% |

### In / out of the MVP (M1 + M2)

| In | Out (later) |
|---|---|
| Learning path chapters 1–4, daily puzzle | Chapters 5–7 (M3 → launch) |
| AI ladder levels 1–6 | Levels 7–8 (need a stronger search) |
| Pass-and-play, online quick match, friend challenge, replays | Tournaments, clubs, spectating, correspondence |
| Glicko-2 ratings, leaderboard | Seasons, rating tiers art |
| Preset emotes | Free chat (maybe never) |
| Board theme "Desert Night" only | Cosmetic store |
| Admin: balance dashboard, players (ban), matches | Content management UI (lessons are JSON files in the repo until then) |
| Arabic + English | More languages (French, Urdu, Turkish, Indonesian are candidates) |

### Success metrics (tracked from M1)
- Tutorial completion, first-game win rate, lessons per user, daily-puzzle participation.
- Games per day per player, median game length (target 5–10 min), share of games ending by each condition.
- D1/D7/D30 retention, session length, crash-free rate.
- Online: matchmaking time, abandon rate, live South score (per rules version).
- Coach: review opens, "helpful" taps, reports, cost per review.

## 12. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Supply is hard to read for new players | The glow, dry mark and ripple; lesson chapter 2; the playtest decision rule (> 30% wrong → fix the UI first) |
| First-player advantage with humans | Live telemetry per rules version; v0.7 and the pie rule ready (`docs/balance-log.md`) |
| A dominant opening found by the community | Telemetry on first moves; a rules version bump is cheap because the rules are versioned per game |
| Too few online players at launch | AI ladder and puzzles carry the game offline; friend challenges by link; matchmaking widens fast, and an "AI stand-in" is offered after 30 s, clearly labelled, never disguised |
| LLM coach cost or quality | Engine facts in, explanation out; evals in CI; per-user quotas; template fallback |
| Cheating with an engine | Server-side validation; analysis after the beta; reports |
| Originality / IP | Concept review in `docs/concepts.md`; trademark search before launch (name and logo) |

## 13. Provisional decisions (〔P〕) and what decides them

| Decision | Current choice | Decided by |
|---|---|---|
| Rules version | v0.6 | Playtest Q4 (South ≥ 60% of 20+ human games → v0.7 or pie rule) |
| Opening theory lesson (Jundi to the Well) | Included in chapter 6 | Playtest Q5: do humans find it? Is it too strong? |
| Ply limit 60 / water 10 | Kept | Playtest Q6/Q8 (too long → test 8 water in the lab) |
| The Rami in the first game | Included | Playtest Q1/Q7 (teaching > 7 min → hold the Rami back until lesson 4) |
| AI ladder depths and noise | Section 5 table | Lab Elo ladder + first-game win-rate telemetry |
| Persona style biases | Planned | Playtests of levels 3–6 ("do they feel different?") |
| Time control 4 + 2 | Kept | Beta telemetry: median game length and timeouts |
| Reconnect forfeit after 60 s | Kept | Beta: abandon complaints vs. waiting complaints |
| Rating tier names | Sand → Fortress | Player survey in the beta |
| Monetization | None at MVP | After the beta, with cost per coach review |

## 14. Open questions for the next review
1. Should guest progress be stored on the device only, or synced anonymously and merged at sign-in? (Proposal: an anonymous account with an upgrade path.)
2. Do we want a "Learn with a friend" mode, where pass-and-play shows coach tips for both sides?
3. Should school or classroom features (teacher dashboard, puzzle sets) be considered for an education version?
