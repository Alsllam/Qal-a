import 'package:qala/features/game/domain/entities/opponent.dart';
import 'package:qala/features/ladder/domain/entities/ladder_entry.dart';
import 'package:qala/features/ladder/domain/repositories/ladder_repository.dart';

class GetLadder {
  const new(this._repository);

  final LadderRepository _repository;

  List<LadderEntry> call() {
    final beaten = _repository.highestBeaten();
    return [
      for (final o in Opponents.ladder)
        LadderEntry(
          opponent: o,
          unlocked: o.level <= beaten + 1,
          beaten: o.level <= beaten,
        ),
    ];
  }
}

class RecordLadderWin {
  const new(this._repository);

  final LadderRepository _repository;

  /// Records a win; returns true when it unlocked a new level.
  Future<bool> call(int level) async {
    if (level <= _repository.highestBeaten()) return false;
    await _repository.saveHighestBeaten(level);
    return level < Opponents.ladder.last.level;
  }
}
