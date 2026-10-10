import 'package:game_core/game_core.dart';

import 'package:qala/features/game/domain/entities/opponent.dart';

/// Chooses a move for an AI opponent. Implemented in the data layer (runs
/// the search off the UI thread).
///
/// The domain layer depends on `game_core`: the rules engine is pure Dart and
/// *is* the game's domain model, shared with the balance lab and the server.
abstract interface class AiMoveService {
  Future<Move> chooseMove(GameState position, Opponent opponent, int seed);
}
