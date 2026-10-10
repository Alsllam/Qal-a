import 'package:equatable/equatable.dart';

/// A finished game, stored locally for stats.
class GameResult extends Equatable {
  const new({
    required this.mode,
    required this.winner,
    required this.reason,
    required this.plies,
    required this.rulesVersion,
    required this.finishedAt,
    this.opponentLevel,
    this.humanSide,
  });

  /// `pvp` or `ladder`.
  final String mode;

  /// `south`, `north` or null for a draw.
  final String? winner;
  final String reason;
  final int plies;
  final String rulesVersion;
  final DateTime finishedAt;
  final int? opponentLevel;
  final String? humanSide;

  bool get humanWon => humanSide != null && humanSide == winner;

  @override
  List<Object?> get props => [
    mode,
    winner,
    reason,
    plies,
    rulesVersion,
    finishedAt,
    opponentLevel,
    humanSide,
  ];
}
