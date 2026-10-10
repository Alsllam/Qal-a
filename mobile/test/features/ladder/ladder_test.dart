import 'package:flutter_test/flutter_test.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/game/domain/entities/opponent.dart';
import 'package:qala/features/ladder/data/repositories/ladder_repository_impl.dart';
import 'package:qala/features/ladder/domain/usecases/ladder_usecases.dart';

void main() {
  late LadderRepositoryImpl repo;

  setUp(() => repo = LadderRepositoryImpl(MemoryKeyValueStore()));

  test('only the first opponent is unlocked at the start', () {
    final ladder = GetLadder(repo)();
    expect(ladder, hasLength(Opponents.ladder.length));
    expect(ladder.first.unlocked, isTrue);
    expect(ladder.skip(1).any((e) => e.unlocked), isFalse);
  });

  test('beating a level unlocks the next one, once', () async {
    final record = RecordLadderWin(repo);
    expect(await record(1), isTrue);
    expect(await record(1), isFalse, reason: 'already beaten');
    final ladder = GetLadder(repo)();
    expect(ladder[0].beaten, isTrue);
    expect(ladder[1].unlocked, isTrue);
    expect(ladder[2].unlocked, isFalse);
  });

  test('opponents get stronger level by level', () {
    for (var i = 1; i < Opponents.ladder.length; i++) {
      final a = Opponents.ladder[i - 1];
      final b = Opponents.ladder[i];
      expect(
        b.depth > a.depth || (b.depth == a.depth && b.noise < a.noise),
        isTrue,
        reason: '${b.nameEn} must be stronger than ${a.nameEn}',
      );
    }
  });
}
