# game_core

Pure Dart rules engine for **Qal'a: Wells & Walls**. It has no Flutter dependency, so the mobile app, the balance lab and the backend can all share the exact same rules.

The rules themselves are in [`docs/rules.md`](../../docs/rules.md).

## Usage

```dart
import 'package:game_core/game_core.dart';

var state = GameState.initial();           // RuleSet.standard (v0.1)
print(state.legalMoves);                   // [b1-b2, b1-b3, …]
state = state.play(Move.parse('b1-b4'));   // immutable: returns a new state
print(state.isSupplied(Square.parse('b4'))); // false: the Faris left its chain
print(state.toAscii());                    // ' marks unsupplied pieces
if (state.isOver) print(state.outcome);
```

- `GameState.fromNotation('1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0')` loads a position, and `toNotation()` writes one.
- `RuleSet` holds every tunable number (ply limit, Faris range, shot distance, Wells, Amir-as-source, setup), so the balance lab can try variants with `RuleSet.standard.copyWith(...)`.
- `playUnchecked` skips the legality check, for search code that only plays moves taken from `legalMoves`.

## Development

```sh
dart pub get
dart analyze
dart test
```

The tests cover every piece's movement, supply (chains, Wells, Qal'a, Amir), every win and end condition, notation, a perft fingerprint of the move generator, and 300 random games that check the engine's invariants on every move.
