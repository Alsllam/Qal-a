import 'package:equatable/equatable.dart';
import 'package:game_core/game_core.dart';

import 'package:qala/features/game/domain/entities/opponent.dart';

/// How a game is played.
sealed class GameMode extends Equatable {
  const new();
}

/// Two people on one device.
final class PassAndPlay extends GameMode {
  const new();

  @override
  List<Object?> get props => const [];
}

/// The human against a ladder opponent.
final class LadderGame extends GameMode {
  const new(this.opponent, {this.humanSide = Side.south});

  final Opponent opponent;
  final Side humanSide;

  Side get aiSide => humanSide.opponent;

  @override
  List<Object?> get props => [opponent, humanSide];
}
