import 'dart:math';

import 'package:flutter/foundation.dart';
import 'package:game_ai/game_ai.dart';
import 'package:game_core/game_core.dart';

import 'package:qala/features/game/domain/entities/opponent.dart';
import 'package:qala/features/game/domain/services/ai_move_service.dart';

/// Runs `game_ai` in a background isolate (`compute`).
class IsolateAiMoveService implements AiMoveService {
  const new();

  @override
  Future<Move> chooseMove(GameState position, Opponent opponent, int seed) =>
      compute(_search, (position, opponent.depth, opponent.noise, seed));
}

Move _search((GameState, int, double, int) request) {
  final (position, depth, noise, seed) = request;
  return AlphaBetaPlayer(
    depth: depth,
    noise: noise,
  ).choose(position, Random(seed));
}
