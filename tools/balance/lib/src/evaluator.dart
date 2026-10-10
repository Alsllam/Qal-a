import 'dart:math';

import 'package:game_core/game_core.dart';

/// Weights of the heuristic evaluation.
class EvalWeights {
  const EvalWeights({
    this.jundi = 1.0,
    this.faris = 3.0,
    this.rami = 2.5,
    this.supplied = 0.3,
    this.well = 0.8,
    this.advance = 0.08,
  });

  final double jundi;
  final double faris;
  final double rami;

  /// Bonus per supplied piece (Amir excluded).
  final double supplied;

  /// Bonus per Well held.
  final double well;

  /// Bonus per step closer to the enemy Qal'a, for supplied non-Amir pieces.
  final double advance;

  double valueOf(PieceType type) => switch (type) {
        PieceType.amir => 0,
        PieceType.jundi => jundi,
        PieceType.faris => faris,
        PieceType.rami => rami,
      };
}

/// Static evaluation of a position. Symmetric: mirroring the board and
/// swapping sides gives the same score for the mirrored side.
class Evaluator {
  const Evaluator([this.weights = const EvalWeights()]);

  /// Score of a won game (minus the distance to the win).
  static const double win = 100000;

  final EvalWeights weights;

  /// Score from [side]'s point of view: positive is good for [side].
  double evaluate(GameState state, Side side) {
    final outcome = state.outcome;
    if (outcome != null) {
      if (outcome.isDraw) return 0;
      return outcome.winner == side ? win : -win;
    }
    return _sideScore(state, side) - _sideScore(state, side.opponent);
  }

  double _sideScore(GameState state, Side side) {
    final enemyQala = state.rules.qalaOf(side.opponent);
    var score = weights.well * state.wellsHeld(side);
    for (final (square, piece) in state.pieces(side)) {
      score += weights.valueOf(piece.type);
      if (piece.type == PieceType.amir || !state.isSupplied(square)) continue;
      score += weights.supplied;
      final distance = max(
        (square.file - enemyQala.file).abs(),
        (square.rank - enemyQala.rank).abs(),
      );
      score += weights.advance * (boardSize - 1 - distance);
    }
    return score;
  }
}
