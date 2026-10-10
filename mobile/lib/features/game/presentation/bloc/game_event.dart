part of 'game_bloc.dart';

sealed class GameEvent extends Equatable {
  const new();

  @override
  List<Object?> get props => const [];
}

final class GameStarted extends GameEvent {
  const new();
}

final class SquareTapped extends GameEvent {
  const new(this.square);

  final Square square;

  @override
  List<Object?> get props => [square];
}

final class UndoPressed extends GameEvent {
  const new();
}

final class RestartPressed extends GameEvent {
  const new();
}

final class _AiMoveReady extends GameEvent {
  const new(this.move, this.generation);

  final Move move;
  final int generation;

  @override
  List<Object?> get props => [move, generation];
}
