import 'dart:math';

import 'package:game_ai/game_ai.dart';
import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

GameState pos(String board, [Side toMove = Side.south]) =>
    GameState.fromNotation('$board ${toMove.code} 0');

/// Mirrors the board top-to-bottom and swaps the colours.
GameState mirror(GameState state) {
  final rows = state.toNotation().split(' ').first.split('/').reversed;
  String swapCase(String s) => s
      .split('')
      .map((c) => c == c.toUpperCase() ? c.toLowerCase() : c.toUpperCase())
      .join();
  return pos(rows.map(swapCase).join('/'), state.toMove.opponent);
}

void main() {
  group('Evaluator', () {
    const evaluator = Evaluator();

    test('the opening position scores 0', () {
      expect(evaluator.evaluate(GameState.initial(), Side.south),
          closeTo(0, 1e-9));
    });

    test('is symmetric under mirroring', () {
      final random = Random(5);
      var state = GameState.initial();
      for (var i = 0; i < 30 && !state.isOver; i++) {
        final moves = state.legalMoves;
        state = state.play(moves[random.nextInt(moves.length)]);
        final mirrored = mirror(state);
        expect(
          evaluator.evaluate(mirrored, Side.north),
          closeTo(evaluator.evaluate(state, Side.south), 1e-9),
          reason: state.toNotation(),
        );
      }
    });

    test('values extra material', () {
      final up = pos('3a3/7/7/7/7/7/2FA3');
      expect(evaluator.evaluate(up, Side.south), greaterThan(0));
      expect(evaluator.evaluate(up, Side.north), lessThan(0));
    });

    test('finished games score as wins and losses', () {
      final over = pos('7/7/7/7/7/3a3/3A3').play(Move.parse('d1xd2'));
      expect(evaluator.evaluate(over, Side.south), Evaluator.win);
      expect(evaluator.evaluate(over, Side.north), -Evaluator.win);
    });
  });

  group('AlphaBetaPlayer', () {
    final random = Random(1);

    test('takes the Amir when it can', () {
      // South Faris (supplied by its Amir on b3) can capture the Amir on c6.
      final state = pos('7/2a4/7/2F4/1A5/7/7');
      expect(
          AlphaBetaPlayer(depth: 2).choose(state, random), Move.parse('c4xc6'));
    });

    test('walks into the open enemy Qal\'a to win', () {
      final state = pos('a6/3A3/7/7/7/7/7');
      expect(
          AlphaBetaPlayer(depth: 1).choose(state, random), Move.parse('d6-d7'));
    });

    test('moves its Amir out of an attack', () {
      // North Faris on the c4 Well (supplied) threatens c4xc1.
      final state = pos('6a/7/7/2f4/7/7/2A4');
      final after = state.play(AlphaBetaPlayer(depth: 2).choose(state, random));
      final amir = after.amirSquare(Side.south)!;
      expect(after.legalMoves.where((m) => m.to == amir), isEmpty);
    });

    test('prefers winning material', () {
      // South Rami (supplied by its Amir) can shoot the Faris on d6.
      final state = pos('6a/3f3/7/3RA2/7/7/7');
      expect(
          AlphaBetaPlayer(depth: 2).choose(state, random), Move.parse('d4*d6'));
    });

    test('noise only picks moves close to the best', () {
      final state = pos('7/2a4/7/2F4/1A5/7/7');
      final player = AlphaBetaPlayer(depth: 2, noise: 0.5);
      for (var seed = 0; seed < 20; seed++) {
        expect(player.choose(state, Random(seed)), Move.parse('c4xc6'));
      }
    });

    test('always returns a legal move', () {
      final player = AlphaBetaPlayer(depth: 2, noise: 0.2);
      final random = Random(3);
      var state = GameState.initial();
      while (!state.isOver) {
        final move = player.choose(state, random);
        expect(state.isLegal(move), isTrue);
        state = state.play(move);
      }
    });
  });
}
