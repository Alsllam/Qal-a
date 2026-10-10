import 'package:flutter_test/flutter_test.dart';
import 'package:game_core/game_core.dart';
import 'package:qala/core/widgets/board/board_view_model.dart';

Square sq(String n) => Square.parse(n);

void main() {
  final opening = GameState.initial();

  test('tapping an own piece selects it', () {
    expect(
      BoardSelection.tap(opening, null, sq('b1')),
      SelectionResult.select(sq('b1')),
    );
  });

  test('tapping a legal target returns the move', () {
    expect(
      BoardSelection.tap(opening, sq('b1'), sq('b4')),
      SelectionResult.move(Move.parse('b1-b4')),
    );
  });

  test('tapping the selected piece again clears the selection', () {
    expect(
      BoardSelection.tap(opening, sq('b1'), sq('b1')),
      const SelectionResult.select(null),
    );
  });

  test('a cut-off piece tapping an enemy is rejected for lack of water', () {
    // South Faris b4 is alone; North Jundi b5 next to it.
    final state = GameState.fromNotation('6a/7/1j5/1F5/7/7/3A3 s 0 0:0');
    expect(
      BoardSelection.tap(state, sq('b4'), sq('b5')),
      const SelectionResult.rejected(noWater: true),
    );
  });

  test('a supplied piece tapping an unreachable enemy is just not allowed', () {
    final state = GameState.fromNotation('6a/7/7/2j4/7/2J4/3A3 s 0 0:0');
    expect(
      BoardSelection.tap(state, sq('c2'), sq('c4')),
      const SelectionResult.rejected(noWater: false),
    );
  });
}
