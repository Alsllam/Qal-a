import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/ladder/domain/repositories/ladder_repository.dart';

class LadderRepositoryImpl implements LadderRepository {
  new(this._store);

  final KeyValueStore _store;
  static const _key = 'ladder.highestBeaten';

  @override
  int highestBeaten() => _store.getInt(_key) ?? 0;

  @override
  Future<void> saveHighestBeaten(int level) => _store.setInt(_key, level);
}
