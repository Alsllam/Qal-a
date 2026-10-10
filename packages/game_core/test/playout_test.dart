import 'dart:math';

import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

/// Number of leaf positions [depth] plies ahead (a regression fingerprint of
/// the move generator).
int perft(GameState state, int depth) {
  if (depth == 0 || state.isOver) return 1;
  var total = 0;
  for (final move in state.legalMoves) {
    total += perft(state.playUnchecked(move), depth - 1);
  }
  return total;
}

void main() {
  test('perft from the opening position', () {
    final state = GameState.initial();
    expect(perft(state, 1), 14);
    // 14 × 14: in the opening no move changes the other side's options.
    expect(perft(state, 2), 196);
    expect(perft(state, 3), 3302);
    expect(perft(state, 4), 55418);
  });

  test('perft under the v0.1 rules', () {
    final state = GameState.initial(RuleSet.v0_1);
    expect(perft(state, 1), 14);
    // 196 plus North's Rami gaining a diagonal shot at b4 after b1-b4 and at
    // f4 after f1-f4.
    expect(perft(state, 2), 198);
    expect(perft(state, 3), 3350);
    expect(perft(state, 4), 56618);
  });

  test('random games always end and keep every invariant', () {
    final random = Random(2026);
    final reasons = <EndReason, int>{};
    for (var game = 0; game < 300; game++) {
      var state = GameState.initial();
      var previousCount = 16;
      while (!state.isOver) {
        final legal = state.legalMoves;
        expect(legal, isNotEmpty, reason: 'ongoing game must have moves');
        for (final move in legal) {
          expect(state.at(move.from)?.side, state.toMove);
          expect(state.at(move.to)?.side, isNot(state.toMove));
          expect(state.at(move.to) == null, move.kind == MoveKind.step);
        }
        final move = legal[random.nextInt(legal.length)];
        final next = state.play(move);
        expect(next.ply, state.ply + 1);
        expect(next.toMove, state.toMove.opponent);

        final count = next.pieceCount(Side.south) + next.pieceCount(Side.north);
        expect(count, previousCount - (move.removesPiece ? 1 : 0));
        previousCount = count;

        if (!next.isOver) {
          final reparsed = GameState.fromNotation(next.toNotation());
          expect(reparsed.legalMoves.toSet(), next.legalMoves.toSet());
        }
        state = next;
      }
      expect(state.ply, lessThanOrEqualTo(RuleSet.standard.plyLimit));
      final outcome = state.outcome!;
      if (outcome.reason == EndReason.amirCaptured) {
        expect(state.amirSquare(outcome.winner!.opponent), isNull);
      } else {
        expect(state.amirSquare(Side.south), isNotNull);
        expect(state.amirSquare(Side.north), isNotNull);
      }
      reasons.update(outcome.reason, (n) => n + 1, ifAbsent: () => 1);
    }
    expect(reasons.values.fold(0, (a, b) => a + b), 300);
  });
}
