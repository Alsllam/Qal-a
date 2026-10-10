import 'package:fpdart/fpdart.dart';

import 'package:qala/core/error/failure.dart';
import 'package:qala/features/game/domain/entities/game_result.dart';

abstract interface class GameHistoryRepository {
  Future<Either<Failure, Unit>> save(GameResult result);
  Future<Either<Failure, List<GameResult>>> recent({int limit = 50});
}
