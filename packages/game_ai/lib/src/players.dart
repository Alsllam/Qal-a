import 'dart:math';

import 'package:game_core/game_core.dart';

/// Something that picks a move. [random] is the only source of randomness so
/// that matches are reproducible from a seed.
abstract interface class Player {
  String get name;

  Move choose(GameState state, Random random);
}

/// Picks a uniformly random legal move. Baseline and opening randomiser.
class RandomPlayer implements Player {
  const RandomPlayer();

  @override
  String get name => 'random';

  @override
  Move choose(GameState state, Random random) {
    final moves = state.legalMoves;
    return moves[random.nextInt(moves.length)];
  }
}
