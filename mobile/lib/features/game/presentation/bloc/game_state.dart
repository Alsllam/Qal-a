part of 'game_bloc.dart';

/// One-shot effects consumed by the page's BlocListener.
sealed class GameEffect extends Equatable {
  const new();

  @override
  List<Object?> get props => const [];
}

final class NoWaterHint extends GameEffect {
  const new();
}

final class NotAllowedHint extends GameEffect {
  const new();
}

final class GameFinished extends GameEffect {
  const new(this.outcome, {required this.unlockedNext});

  final Outcome outcome;

  /// A ladder win that unlocked the next opponent.
  final bool unlockedNext;

  @override
  List<Object?> get props => [outcome, unlockedNext];
}

class PlayState extends Equatable {
  const new({
    required this.mode,
    required this.position,
    this.history = const [],
    this.moves = const [],
    this.selected,
    this.aiThinking = false,
    this.effect,
    this.effectSeq = 0,
  });

  final GameMode mode;
  final GameState position;
  final List<GameState> history;
  final List<Move> moves;
  final Square? selected;
  final bool aiThinking;
  final GameEffect? effect;

  /// Increments with every new effect, so equal effects still notify.
  final int effectSeq;

  Move? get lastMove => moves.isEmpty ? null : moves.last;

  List<Move> get targets =>
      selected == null ? const [] : position.legalMovesFrom(selected!);

  bool get isHumanTurn => switch (mode) {
    PassAndPlay() => !position.isOver,
    LadderGame(:final humanSide) =>
      !position.isOver && position.toMove == humanSide,
  };

  bool get canUndo => !aiThinking && _pliesToUndo > 0;

  int get _pliesToUndo {
    if (history.isEmpty) return 0;
    return switch (mode) {
      PassAndPlay() => 1,
      // Back to the most recent position where the human was to move.
      LadderGame(:final humanSide) => () {
        for (var i = history.length - 1; i >= 0; i--) {
          if (history[i].toMove == humanSide) return history.length - i;
        }
        return 0;
      }(),
    };
  }

  PlayState copyWith({
    GameState? position,
    List<GameState>? history,
    List<Move>? moves,
    Square? Function()? selected,
    bool? aiThinking,
    GameEffect? effect,
  }) => PlayState(
    mode: mode,
    position: position ?? this.position,
    history: history ?? this.history,
    moves: moves ?? this.moves,
    selected: selected == null ? this.selected : selected(),
    aiThinking: aiThinking ?? this.aiThinking,
    effect: effect ?? this.effect,
    effectSeq: effect == null ? effectSeq : effectSeq + 1,
  );

  @override
  List<Object?> get props => [
    mode,
    position,
    history.length,
    moves,
    selected,
    aiThinking,
    effect,
    effectSeq,
  ];
}
