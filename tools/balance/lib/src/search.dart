import 'dart:math';

import 'package:game_core/game_core.dart';

import 'evaluator.dart';
import 'players.dart';

/// Negamax alpha-beta player with capture quiescence.
///
/// [noise] adds variety without blunders: the player picks randomly among the
/// moves scoring within [noise] of the best move.
class AlphaBetaPlayer implements Player {
  AlphaBetaPlayer({
    this.depth = 2,
    this.noise = 0.0,
    this.quiescenceDepth = 4,
    this.evaluator = const Evaluator(),
  }) : assert(depth >= 1);

  final int depth;
  final double noise;
  final int quiescenceDepth;
  final Evaluator evaluator;

  /// Nodes visited since construction (for performance reports).
  int nodes = 0;

  @override
  String get name => 'alphabeta(d=$depth, noise=$noise)';

  @override
  Move choose(GameState state, Random random) {
    final moves = List.of(state.legalMoves)..shuffle(random);
    _order(state, moves);
    if (moves.length == 1) return moves.single;

    var best = double.negativeInfinity;
    final exact = <(Move, double)>[];
    for (final move in moves) {
      final floor = best - noise;
      final score = -_negamax(
        state.playUnchecked(move),
        depth - 1,
        double.negativeInfinity,
        -floor,
        1,
      );
      if (score > floor || floor == double.negativeInfinity) {
        exact.add((move, score));
        best = max(best, score);
      }
    }
    final candidates = [
      for (final (move, score) in exact)
        if (score >= best - noise) move,
    ];
    return candidates[random.nextInt(candidates.length)];
  }

  /// Best score for the side to move in [state], from its point of view.
  double search(GameState state) =>
      _negamax(state, depth, double.negativeInfinity, double.infinity, 0);

  double _negamax(
    GameState state,
    int depth,
    double alpha,
    double beta,
    int ply,
  ) {
    nodes++;
    if (state.isOver) return _terminal(state, ply);
    if (depth <= 0) {
      return _quiescence(state, alpha, beta, ply, quiescenceDepth);
    }

    final moves = List.of(state.legalMoves);
    _order(state, moves);
    for (final move in moves) {
      final score = -_negamax(
          state.playUnchecked(move), depth - 1, -beta, -alpha, ply + 1);
      if (score >= beta) return beta;
      if (score > alpha) alpha = score;
    }
    return alpha;
  }

  double _quiescence(
    GameState state,
    double alpha,
    double beta,
    int ply,
    int remaining,
  ) {
    nodes++;
    if (state.isOver) return _terminal(state, ply);
    final standPat = evaluator.evaluate(state, state.toMove);
    if (remaining == 0) return standPat;
    if (standPat >= beta) return beta;
    if (standPat > alpha) alpha = standPat;

    final captures = state.legalMoves.where((m) => m.removesPiece).toList();
    _order(state, captures);
    for (final move in captures) {
      final score = -_quiescence(
          state.playUnchecked(move), -beta, -alpha, ply + 1, remaining - 1);
      if (score >= beta) return beta;
      if (score > alpha) alpha = score;
    }
    return alpha;
  }

  /// Wins sooner and losses later are preferred.
  double _terminal(GameState state, int ply) {
    final winner = state.outcome!.winner;
    if (winner == null) return 0;
    return winner == state.toMove ? Evaluator.win - ply : -Evaluator.win + ply;
  }

  /// Captures first (most valuable victim first), then quiet moves. Stable,
  /// so a shuffled input keeps a random order among equals.
  void _order(GameState state, List<Move> moves) {
    double key(Move m) {
      if (!m.removesPiece) return 0;
      final victim = state.at(m.to)!.type;
      return victim == PieceType.amir
          ? 1000
          : 1 + evaluator.weights.valueOf(victim);
    }

    final keyed = [for (final m in moves) (m, key(m))];
    _stableSortDescending(keyed);
    for (var i = 0; i < moves.length; i++) {
      moves[i] = keyed[i].$1;
    }
  }

  static void _stableSortDescending(List<(Move, double)> list) {
    // Insertion sort: lists are short (< 60) and it is stable.
    for (var i = 1; i < list.length; i++) {
      final item = list[i];
      var j = i - 1;
      while (j >= 0 && list[j].$2 < item.$2) {
        list[j + 1] = list[j];
        j--;
      }
      list[j + 1] = item;
    }
  }
}
