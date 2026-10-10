import 'package:balance/balance.dart';
import 'package:test/test.dart';

void main() {
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
