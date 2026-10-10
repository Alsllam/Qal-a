import 'dart:async';

import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:game_core/game_core.dart';
import 'package:qala/core/widgets/board/board_view_model.dart';
import 'package:qala/features/game/domain/entities/game_mode.dart';
import 'package:qala/features/game/domain/entities/game_result.dart';
import 'package:qala/features/game/domain/usecases/game_usecases.dart';
import 'package:qala/features/ladder/domain/usecases/ladder_usecases.dart';

part 'game_event.dart';
part 'game_state.dart';

/// One game: human taps, AI turns, undo, restart, result saving.
class GameBloc extends Bloc<GameEvent, PlayState> {
  new({
    required GameMode mode,
    required this._chooseAiMove,
    required this._saveGameResult,
    required this._recordLadderWin,
    RuleSet rules = RuleSet.standard,
    String? startPosition,
    this.aiMinDelay = const Duration(milliseconds: 450),
    DateTime Function()? clock,
  }) : _rules = rules,
       _clock = clock ?? DateTime.now,
       _start = startPosition,
       super(PlayState(mode: mode, position: _initial(rules, startPosition))) {
    on<GameStarted>((_, emit) => _maybeStartAi(emit));
    on<SquareTapped>(_onTap);
    on<UndoPressed>(_onUndo);
    on<RestartPressed>(_onRestart);
    on<_AiMoveReady>(_onAiMove);
  }

  final ChooseAiMove _chooseAiMove;
  final SaveGameResult _saveGameResult;
  final RecordLadderWin _recordLadderWin;
  final RuleSet _rules;
  final DateTime Function() _clock;
  final Duration aiMinDelay;
  final String? _start;
  int _generation = 0;

  static GameState _initial(RuleSet rules, String? start) => start == null
      ? GameState.initial(rules)
      : GameState.fromNotation(start, rules: rules);

  Future<void> _onTap(SquareTapped event, Emitter<PlayState> emit) async {
    if (!state.isHumanTurn || state.aiThinking) return;
    final result = BoardSelection.tap(
      state.position,
      state.selected,
      event.square,
    );
    if (result.move != null) {
      await _play(result.move!, emit);
    } else if (result.rejected) {
      emit(
        state.copyWith(
          effect: result.noWater ? const NoWaterHint() : const NotAllowedHint(),
        ),
      );
    } else {
      emit(state.copyWith(selected: () => result.selected));
    }
  }

  Future<void> _play(Move move, Emitter<PlayState> emit) async {
    final next = state.position.play(move);
    emit(
      state.copyWith(
        position: next,
        history: [...state.history, state.position],
        moves: [...state.moves, move],
        selected: () => null,
      ),
    );
    if (next.isOver) {
      await _finish(next.outcome!, emit);
    } else {
      _maybeStartAi(emit);
    }
  }

  void _maybeStartAi(Emitter<PlayState> emit) {
    final mode = state.mode;
    if (mode is! LadderGame ||
        state.position.isOver ||
        state.position.toMove != mode.aiSide) {
      return;
    }
    final generation = _generation;
    final position = state.position;
    final started = _clock();
    emit(state.copyWith(aiThinking: true));
    unawaited(() async {
      final result = await _chooseAiMove(
        position,
        mode.opponent,
        state.moves.length + 1,
      );
      final wait = aiMinDelay - _clock().difference(started);
      if (wait > Duration.zero) await Future<void>.delayed(wait);
      result.match((_) => null, (move) {
        if (!isClosed) add(_AiMoveReady(move, generation));
      });
    }());
  }

  Future<void> _onAiMove(_AiMoveReady event, Emitter<PlayState> emit) async {
    if (event.generation != _generation || !state.aiThinking) return;
    emit(state.copyWith(aiThinking: false));
    await _play(event.move, emit);
  }

  Future<void> _finish(Outcome outcome, Emitter<PlayState> emit) async {
    final mode = state.mode;
    final humanSide = mode is LadderGame ? mode.humanSide : null;
    await _saveGameResult(
      GameResult(
        mode: mode is LadderGame ? 'ladder' : 'pvp',
        winner: outcome.winner?.name,
        reason: outcome.reason.name,
        plies: state.position.ply,
        rulesVersion: _rules.version,
        finishedAt: _clock(),
        opponentLevel: mode is LadderGame ? mode.opponent.level : null,
        humanSide: humanSide?.name,
      ),
    );
    var unlocked = false;
    if (mode is LadderGame && outcome.winner == mode.humanSide) {
      unlocked = await _recordLadderWin(mode.opponent.level);
    }
    emit(state.copyWith(effect: GameFinished(outcome, unlockedNext: unlocked)));
  }

  void _onUndo(UndoPressed event, Emitter<PlayState> emit) {
    if (!state.canUndo) return;
    final plies = state._pliesToUndo;
    final history = [...state.history];
    final moves = [...state.moves];
    var position = state.position;
    for (var i = 0; i < plies; i++) {
      position = history.removeLast();
      moves.removeLast();
    }
    _generation++;
    emit(
      state.copyWith(
        position: position,
        history: history,
        moves: moves,
        selected: () => null,
        aiThinking: false,
      ),
    );
  }

  void _onRestart(RestartPressed event, Emitter<PlayState> emit) {
    _generation++;
    emit(PlayState(mode: state.mode, position: _initial(_rules, _start)));
    _maybeStartAi(emit);
  }
}
