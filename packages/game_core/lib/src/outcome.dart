import 'side.dart';

/// Why a game ended.
enum EndReason {
  /// The winner captured (or shot) the enemy Amir.
  amirCaptured,

  /// The winner ended a turn with a supplied piece on the enemy Qal'a.
  qalaTaken,

  /// The loser had no legal move on their turn.
  noLegalMoves,

  /// Ply limit reached; the winner held more Wells.
  plyLimitWells,

  /// Ply limit reached; Wells tied, the winner had more pieces.
  plyLimitMaterial,

  /// Ply limit reached with Wells and pieces tied.
  plyLimitDraw,
}

/// The result of a finished game.
class Outcome {
  const Outcome(this.winner, this.reason);

  /// Null for a draw.
  final Side? winner;
  final EndReason reason;

  bool get isDraw => winner == null;

  @override
  bool operator ==(Object other) =>
      other is Outcome && other.winner == winner && other.reason == reason;

  @override
  int get hashCode => Object.hash(winner, reason);

  @override
  String toString() => isDraw
      ? 'Draw (${reason.name})'
      : '${winner!.name} wins (${reason.name})';
}
