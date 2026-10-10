import 'package:fpdart/fpdart.dart';
import 'package:qala/core/error/failure.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/learn/data/datasources/lesson_asset_data_source.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';
import 'package:qala/features/learn/domain/repositories/lesson_repository.dart';

class LessonRepositoryImpl implements LessonRepository {
  new(this._assets, this._store);

  final LessonAssetDataSource _assets;
  final KeyValueStore _store;
  List<Chapter>? _cache;

  @override
  Future<Either<Failure, List<Chapter>>> chapters() async {
    try {
      return Right(_cache ??= await _assets.load());
    } on Object catch (e) {
      return Left(CacheFailure('$e'));
    }
  }

  @override
  int stars(String lessonId) => _store.getInt('lesson.$lessonId') ?? 0;

  @override
  Future<void> saveStars(String lessonId, int stars) =>
      _store.setInt('lesson.$lessonId', stars);
}
