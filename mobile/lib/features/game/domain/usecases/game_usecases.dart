import 'package:fpdart/fpdart.dart';
import 'package:game_core/game_core.dart';

import 'package:qala/core/error/failure.dart';
import 'package:qala/features/game/domain/entities/game_result.dart';
import 'package:qala/features/game/domain/entities/opponent.dart';
import 'package:qala/features/game/domain/repositories/game_history_repository.dart';
import 'package:qala/features/game/domain/services/ai_move_service.dart';

class ChooseAiMove {
  const new(this._ai);

  final AiMoveService _ai;

  Future<Either<Failure, Move>> call(
    GameState position,
    Opponent opponent,
    int seed,
  ) async {
    if (position.isOver) return const Left(CacheFailure('game over'));
    try {
      final move = await _ai.chooseMove(position, opponent, seed);
      if (!position.isLegal(move)) {
        return Left(CacheFailure('illegal AI move ${move.notation}'));
      }
      return Right(move);
    } on Object catch (e) {
      return Left(CacheFailure('$e'));
    }
  }
}

class SaveGameResult {
  const new(this._repository);

  final GameHistoryRepository _repository;

  Future<Either<Failure, Unit>> call(GameResult result) =>
      _repository.save(result);
}
