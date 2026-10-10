import 'package:fpdart/fpdart.dart';
import 'package:qala/core/error/failure.dart';
import 'package:qala/features/game/data/datasources/game_history_local_data_source.dart';
import 'package:qala/features/game/domain/entities/game_result.dart';
import 'package:qala/features/game/domain/repositories/game_history_repository.dart';

class GameHistoryRepositoryImpl implements GameHistoryRepository {
  new(this._local);

  final GameHistoryLocalDataSource _local;

  @override
  Future<Either<Failure, Unit>> save(GameResult result) async {
    try {
      await _local.add(result);
      return const Right(unit);
    } on Object catch (e) {
      return Left(CacheFailure('$e'));
    }
  }

  @override
  Future<Either<Failure, List<GameResult>>> recent({int limit = 50}) async {
    try {
      return Right(await _local.recent(limit));
    } on Object catch (e) {
      return Left(CacheFailure('$e'));
    }
  }
}
