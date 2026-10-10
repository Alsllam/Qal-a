# game_ai

AI players for **Qal'a: Wells & Walls**, shared by the prototype app (`prototype/`) and the balance lab (`tools/balance`).

- `Evaluator`: heuristic score (material, supply, Wells, water, advance). Mirror-symmetric.
- `AlphaBetaPlayer`: negamax alpha-beta with capture quiescence. `noise` picks randomly among near-best moves, for variety and for weaker levels.
- `RandomPlayer`: a baseline.

```sh
dart pub get && dart test
```
