import 'dart:math';

import 'package:game_core/game_core.dart';

import 'match_runner.dart';

/// Aggregated numbers over a list of games.
class MatchStats {
  MatchStats(this.records) : assert(records.isNotEmpty);

  final List<GameRecord> records;

  int get games => records.length;

  late final int southWins = _count((r) => r.outcome.winner == Side.south);
  late final int northWins = _count((r) => r.outcome.winner == Side.north);
  late final int draws = _count((r) => r.outcome.isDraw);

  /// South's score: wins count 1, draws ½.
  double get southScore => (southWins + draws / 2) / games;

  /// Half-width of the 95% confidence interval of [southScore].
  double get southScoreMargin =>
      1.96 * sqrt(southScore * (1 - southScore) / games);

  double get drawRate => draws / games;

  /// Wins of player A (see [MatchSpec.a]) counting draws as ½.
  double get aScore {
    var score = 0.0;
    for (final r in records) {
      final winner = r.outcome.winner;
      if (winner == null) {
        score += 0.5;
      } else if ((winner == Side.south) == r.aWasSouth) {
        score += 1;
      }
    }
    return score / games;
  }

  late final List<int> _lengths = [for (final r in records) r.plies]..sort();

  double get meanLength => _lengths.reduce((a, b) => a + b) / games;
  int percentileLength(double p) =>
      _lengths[min(games - 1, (p * games).floor())];

  late final Map<EndReason, int> endReasons = {
    for (final reason in EndReason.values)
      reason: _count((r) => r.outcome.reason == reason),
  };

  late final Map<PieceType, int> movesByType = _byType(
    (r) => r.movers,
  );

  late final Map<PieceType, int> capturesByType = _byType((r) => [
        for (var i = 0; i < r.plies; i++)
          if (r.victims[i] != null) r.movers[i],
      ]);

  late final Map<PieceType, int> lostByType =
      _byType((r) => r.victims.whereType<PieceType>());

  late final Map<PieceType, int> winningPieces =
      _byType((r) => [if (r.winningPiece case final p?) p]);

  int get totalMoves => movesByType.values.fold(0, (a, b) => a + b);
  int get totalCaptures => capturesByType.values.fold(0, (a, b) => a + b);

  /// Share of moves made by unsupplied pieces.
  double get unsuppliedMoveShare =>
      records.fold(
          0, (n, r) => n + r.moverWasSupplied.where((s) => !s).length) /
      totalMoves;

  /// Average share of non-Amir pieces that are unsupplied.
  double get unsuppliedPieceShare =>
      records.fold(0.0, (s, r) => s + r.unsuppliedShare) / games;

  /// South's score after each first move: (games, score).
  late final Map<String, (int, double)> byFirstMove = () {
    final grouped = <String, List<GameRecord>>{};
    for (final r in records) {
      grouped.putIfAbsent(r.moves.first.notation, () => []).add(r);
    }
    return {
      for (final MapEntry(:key, :value) in grouped.entries)
        key: (value.length, MatchStats(value).southScore),
    };
  }();

  int _count(bool Function(GameRecord) test) => records.where(test).length;

  Map<PieceType, int> _byType(Iterable<PieceType> Function(GameRecord) pick) {
    final counts = {for (final t in PieceType.values) t: 0};
    for (final r in records) {
      for (final t in pick(r)) {
        counts[t] = counts[t]! + 1;
      }
    }
    return counts;
  }
}
