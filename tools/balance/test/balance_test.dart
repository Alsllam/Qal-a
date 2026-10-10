import 'dart:math';

import 'package:balance/balance.dart';
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
      // South Faris on the c4 Well can capture the Amir on c6.
      final state = pos('7/2a4/7/2F4/7/7/6A');
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
      // South Rami (supplied by the e4 Well chain) can shoot the Faris on d6.
      final state = pos('6a/3f3/7/3RJ2/7/7/A6');
      expect(
          AlphaBetaPlayer(depth: 2).choose(state, random), Move.parse('d4*d6'));
    });

    test('noise only picks moves close to the best', () {
      final state = pos('7/2a4/7/2F4/7/7/6A');
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

  group('match runner and stats', () {
    test('games are reproducible from the seed', () {
      const spec = MatchSpec(rulesVersion: '0.1', games: 3, seed: 9);
      final first = [for (var i = 0; i < 3; i++) playMatchGame(spec, i)];
      final again = [for (var i = 0; i < 3; i++) playMatchGame(spec, i)];
      for (var i = 0; i < 3; i++) {
        expect(again[i].moves, first[i].moves);
      }
    });

    test('runMatch returns one record per game, same as sequential', () async {
      const spec = MatchSpec(rulesVersion: '0.1', games: 6, seed: 4);
      final parallel = await runMatch(spec, workers: 3);
      expect(parallel, hasLength(6));
      for (var i = 0; i < 6; i++) {
        expect(parallel[i].moves, playMatchGame(spec, i).moves);
      }
    });

    test('records describe each ply', () {
      const spec = MatchSpec(rulesVersion: '0.1', games: 1);
      final record = playMatchGame(spec, 0);
      expect(record.movers, hasLength(record.plies));
      expect(record.victims, hasLength(record.plies));
      expect(record.moverWasSupplied, hasLength(record.plies));
      expect(record.unsuppliedShare, inInclusiveRange(0, 1));
    });

    test('alternating sides puts player A on both sides', () {
      const spec = MatchSpec(
        rulesVersion: '0.1',
        games: 2,
        alternateSides: true,
        a: PlayerSpec.random(),
      );
      expect(playMatchGame(spec, 0).aWasSouth, isTrue);
      expect(playMatchGame(spec, 1).aWasSouth, isFalse);
    });

    test('stats add up', () async {
      const spec = MatchSpec(
        rulesVersion: '0.1',
        games: 20,
        a: PlayerSpec.random(),
        b: PlayerSpec.random(),
      );
      final stats = MatchStats(await runMatch(spec, workers: 2));
      expect(stats.southWins + stats.northWins + stats.draws, 20);
      expect(stats.endReasons.values.fold(0, (a, b) => a + b), 20);
      expect(stats.totalMoves, stats.records.fold(0, (n, r) => n + r.plies));
      expect(stats.byFirstMove.values.fold(0, (n, v) => n + v.$1), 20);
      expect(stats.aScore, stats.southScore);
      final checks = runChecks(stats);
      expect(checks, isNotEmpty);
    });

    test('a deeper AI beats a random player', () async {
      const spec = MatchSpec(
        rulesVersion: '0.1',
        games: 20,
        a: PlayerSpec.alphaBeta(depth: 2),
        b: PlayerSpec.random(),
        alternateSides: true,
        randomOpeningPlies: 0,
      );
      final stats = MatchStats(await runMatch(spec, workers: 4));
      expect(stats.aScore, greaterThan(0.9));
    });
  });
}
