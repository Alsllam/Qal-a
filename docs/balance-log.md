# Balance log

This is the changelog of rule versions for **Qal'a: Wells & Walls**. Each entry gives the change, why it was made, and the numbers from the balance lab (`tools/balance`). The full reports are in `tools/balance/reports/v<version>.md`.

## Method

- **AI:** negamax alpha-beta with capture quiescence and a heuristic evaluation (material, supplied pieces, Wells, advance toward the enemy Qal'a). The evaluation is tested to be mirror-symmetric, so the AI favours neither side.
- **Self-play:** 2,000 games per version at search depth 3. The first 2 plies are random, so many openings are explored. Each move is chosen at random among the moves scoring within 0.15 of the best, so games don't repeat.
- **Skill gradient:** 200 games of depth 4 against depth 2, with sides alternating. If the stronger AI wins clearly, the game rewards skill rather than being decided by the first move.
- **Reproducible:** every game has its own seed. Re-running `dart run bin/balance.dart --rules <v> --depth 3` gives the same report, whatever the number of CPU cores.

## Targets

| Check | Target | Why |
|---|---|---|
| First-player balance | South scores 50% ± 5% | Fair for both players |
| Draws | ≤ 10% | Every game should have a winner |
| Game length | 20–50 plies on average | 5–10 minutes on mobile |
| Varied endings | No end reason above 75% | More than one way to win |
| Every piece matters | Each type makes 8–45% of moves | No useless or dominant piece |
| Supply matters | ≥ 5% of pieces unsupplied on average | The core mechanic must actually bite |
| No dominant opening | No first move scores more than 15 points above average (with ≥ 40 games) | No single opening wins by force |
| Skill is rewarded | Depth 4 scores ≥ 70% against depth 2 | Depth worth months of mastery |

## Versions
