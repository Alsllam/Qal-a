# Qal'a (قلعة)

An original turn-based strategy board game rooted in Arab heritage, with a learning path and AI coach. In **Wells & Walls**, two desert fortresses fight over two wells, and an army can only attack while it is linked to water.

## Repository map

| Path | What it is |
|---|---|
| `docs/concepts.md` | Step 1: three game concepts and the choice |
| `docs/rules.md` | The official rules (currently **v0.6**) |
| `docs/balance-log.md` | Every rules version tried in the balance lab, with results |
| `docs/playtest-guide.md` | How to run playtests: what to watch for and what to ask |
| `packages/game_core/` | Pure Dart rules engine (shared by everything) |
| `packages/game_ai/` | AI players (alpha-beta search and evaluation) |
| `tools/balance/` | Balance lab: AI self-play and reports |
| `prototype/` | Flutter + Flame playtest prototype (Arabic/English) |

## Quick start

```sh
cd packages/game_core && dart test                           # rules engine
cd tools/balance && dart run bin/balance.dart --rules 0.6    # balance report
cd prototype && flutter run -d chrome                        # play it
```
