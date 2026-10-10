import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:game_core/game_core.dart';
import 'package:qala/core/error/failure.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/game/domain/entities/game_mode.dart';
import 'package:qala/features/game/domain/entities/game_result.dart';
import 'package:qala/features/game/domain/entities/opponent.dart';
import 'package:qala/features/game/domain/repositories/game_history_repository.dart';
import 'package:qala/features/game/domain/usecases/game_usecases.dart';
import 'package:qala/features/game/presentation/bloc/game_bloc.dart';
import 'package:qala/features/ladder/data/repositories/ladder_repository_impl.dart';
import 'package:qala/features/ladder/domain/usecases/ladder_usecases.dart';

import '../../helpers/fakes.dart';

class MemoryHistory implements GameHistoryRepository {
  final saved = <GameResult>[];

  @override
  Future<Either<Failure, Unit>> save(GameResult result) async {
    saved.add(result);
    return const Right(unit);
  }

  @override
  Future<Either<Failure, List<GameResult>>> recent({int limit = 50}) async =>
      Right(saved);
}

Square sq(String n) => Square.parse(n);

void main() {
  late FirstMoveAi ai;
  late MemoryHistory history;
  late LadderRepositoryImpl ladder;

  setUp(() {
    ai = FirstMoveAi();
    history = MemoryHistory();
    ladder = LadderRepositoryImpl(MemoryKeyValueStore());
  });

  GameBloc build(GameMode mode, {String? start}) => GameBloc(
    mode: mode,
    chooseAiMove: ChooseAiMove(ai),
    saveGameResult: SaveGameResult(history),
    recordLadderWin: RecordLadderWin(ladder),
    startPosition: start,
    aiMinDelay: Duration.zero,
  );

  final shepherd = LadderGame(Opponents.byLevel(1));

  group('pass and play', () {
    blocTest<GameBloc, PlayState>(
      'select then move passes the turn',
      build: () => build(const PassAndPlay()),
      act: (b) => b
        ..add(SquareTapped(sq('b1')))
        ..add(SquareTapped(sq('b4'))),
      verify: (b) {
        expect(b.state.position.toMove, Side.north);
        expect(b.state.lastMove, Move.parse('b1-b4'));
        expect(b.state.selected, isNull);
        expect(b.state.canUndo, isTrue);
      },
    );

    blocTest<GameBloc, PlayState>(
      'a cut-off piece trying to capture produces the no-water hint',
      build: () =>
          build(const PassAndPlay(), start: '6a/7/1j5/1F5/7/7/3A3 s 0 0:0'),
      act: (b) => b
        ..add(SquareTapped(sq('b4')))
        ..add(SquareTapped(sq('b5'))),
      verify: (b) {
        expect(b.state.effect, const NoWaterHint());
        expect(b.state.position.at(sq('b5'))?.side, Side.north);
      },
    );

    blocTest<GameBloc, PlayState>(
      'undo takes back one ply',
      build: () => build(const PassAndPlay()),
      act: (b) => b
        ..add(SquareTapped(sq('c2')))
        ..add(SquareTapped(sq('c3')))
        ..add(const UndoPressed()),
      verify: (b) {
        expect(b.state.position.ply, 0);
        expect(b.state.moves, isEmpty);
      },
    );
  });

  group('ladder', () {
    blocTest<GameBloc, PlayState>(
      'the AI replies after the human move',
      build: () => build(shepherd),
      act: (b) async {
        b
          ..add(const GameStarted())
          ..add(SquareTapped(sq('c2')))
          ..add(SquareTapped(sq('c3')));
        await Future<void>.delayed(const Duration(milliseconds: 20));
      },
      verify: (b) {
        expect(ai.calls, 1);
        expect(b.state.position.ply, 2);
        expect(b.state.position.toMove, Side.south);
        expect(b.state.aiThinking, isFalse);
      },
    );

    blocTest<GameBloc, PlayState>(
      'undo goes back to the human turn',
      build: () => build(shepherd),
      act: (b) async {
        b
          ..add(SquareTapped(sq('c2')))
          ..add(SquareTapped(sq('c3')));
        await Future<void>.delayed(const Duration(milliseconds: 20));
        b.add(const UndoPressed());
      },
      verify: (b) {
        expect(b.state.position.ply, 0);
        expect(b.state.isHumanTurn, isTrue);
      },
    );

    blocTest<GameBloc, PlayState>(
      'winning saves the result and unlocks the next opponent',
      // South Amir d4 can capture the North Amir on d5.
      build: () => build(shepherd, start: '7/7/3a3/3A3/7/7/7 s 0 0:0'),
      act: (b) => b
        ..add(SquareTapped(sq('d4')))
        ..add(SquareTapped(sq('d5'))),
      wait: const Duration(milliseconds: 20),
      verify: (b) {
        final effect = b.state.effect;
        expect(effect, isA<GameFinished>());
        expect((effect! as GameFinished).unlockedNext, isTrue);
        expect(history.saved.single.humanWon, isTrue);
        expect(history.saved.single.reason, 'amirCaptured');
        expect(ladder.highestBeaten(), 1);
      },
    );

    blocTest<GameBloc, PlayState>(
      'the AI moves first when the human plays North',
      build: () =>
          build(LadderGame(Opponents.byLevel(1), humanSide: Side.north)),
      act: (b) => b.add(const GameStarted()),
      wait: const Duration(milliseconds: 20),
      verify: (b) {
        expect(b.state.position.ply, 1);
        expect(b.state.isHumanTurn, isTrue);
      },
    );
  });
}
