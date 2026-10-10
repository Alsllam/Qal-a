import 'package:game_core/game_core.dart';
import 'package:qala/features/game/domain/entities/opponent.dart';
import 'package:qala/features/game/domain/services/ai_move_service.dart';

/// Plays the first legal move instantly.
class FirstMoveAi implements AiMoveService {
  int calls = 0;

  @override
  Future<Move> chooseMove(GameState position, Opponent opponent, int seed) {
    calls++;
    return Future.value(position.legalMoves.first);
  }
}
