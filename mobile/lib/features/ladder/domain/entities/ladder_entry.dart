import 'package:equatable/equatable.dart';

import 'package:qala/features/game/domain/entities/opponent.dart';

class LadderEntry extends Equatable {
  const new({
    required this.opponent,
    required this.unlocked,
    required this.beaten,
  });

  final Opponent opponent;
  final bool unlocked;
  final bool beaten;

  @override
  List<Object?> get props => [opponent, unlocked, beaten];
}
