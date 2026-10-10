# Balance lab

AI self-play for **Qal'a: Wells & Walls**. It plays thousands of games per rules version and writes a report on first- vs second-player win rate, game length, draws, how games end, piece usage, supply, dominant openings, and whether skill (search depth) is rewarded.

The results and every rule change are logged in [`docs/balance-log.md`](../../docs/balance-log.md).

## Run

```sh
dart pub get
dart run bin/balance.dart --rules 0.6 --games 2000 --depth 3 --gradient 200
dart run bin/balance.dart --rules all          # every version in variants.dart
dart run bin/balance.dart --help
```

Reports are written to `reports/v<version>.md`.

## Pieces

| File | What it does |
|---|---|
| `lib/src/evaluator.dart` | Heuristic evaluation (material, supply, Wells, advance). Mirror-symmetric |
| `lib/src/search.dart` | `AlphaBetaPlayer`: negamax alpha-beta, capture quiescence, MVV ordering, noise for variety |
| `lib/src/match_runner.dart` | Plays games on all CPU cores; one seed per game, so results are reproducible |
| `lib/src/stats.dart` | Aggregates game records |
| `lib/src/report.dart` | Balance targets, checks and the Markdown report |
| `lib/src/variants.dart` | Every rules version tried. Add a new one here to test a rule change |

## Trying a rule change

1. If the change needs a new rule parameter, add it to `RuleSet` in `packages/game_core` (with tests).
2. Add a `RuleVariant` to `variants.dart` with a one-line summary.
3. Run the lab and add an entry to `docs/balance-log.md`.
