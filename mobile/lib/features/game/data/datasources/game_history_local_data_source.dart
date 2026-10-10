import 'package:qala/features/game/domain/entities/game_result.dart';
import 'package:sembast/sembast.dart';

/// Finished games in the local Sembast database.
class GameHistoryLocalDataSource {
  new(this._db);

  final Database _db;
  final StoreRef<int, Map<String, Object?>> _store = intMapStoreFactory.store(
    'games',
  );

  Future<void> add(GameResult r) => _store.add(_db, {
    'mode': r.mode,
    'winner': r.winner,
    'reason': r.reason,
    'plies': r.plies,
    'rulesVersion': r.rulesVersion,
    'finishedAt': r.finishedAt.toIso8601String(),
    'opponentLevel': r.opponentLevel,
    'humanSide': r.humanSide,
  });

  Future<List<GameResult>> recent(int limit) async {
    final records = await _store.find(
      _db,
      finder: Finder(sortOrders: [SortOrder(Field.key, false)], limit: limit),
    );
    return [
      for (final r in records)
        GameResult(
          mode: r.value['mode']! as String,
          winner: r.value['winner'] as String?,
          reason: r.value['reason']! as String,
          plies: r.value['plies']! as int,
          rulesVersion: r.value['rulesVersion']! as String,
          finishedAt: DateTime.parse(r.value['finishedAt']! as String),
          opponentLevel: r.value['opponentLevel'] as int?,
          humanSide: r.value['humanSide'] as String?,
        ),
    ];
  }
}
