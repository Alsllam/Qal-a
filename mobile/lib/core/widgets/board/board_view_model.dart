import 'package:equatable/equatable.dart';
import 'package:game_core/game_core.dart';

/// Everything the board needs to draw one frame.
class BoardViewModel extends Equatable {
  const new({
    required this.position,
    this.selected,
    this.targets = const [],
    this.lastMove,
    this.flipped = false,
    this.hintSquares = const {},
  });

  final GameState position;
  final Square? selected;

  /// Legal moves of the selected piece.
  final List<Move> targets;
  final Move? lastMove;

  /// Draw North at the bottom.
  final bool flipped;

  /// Squares to pulse (lesson hints).
  final Set<Square> hintSquares;

  @override
  List<Object?> get props => [
    position,
    selected,
    targets,
    lastMove,
    flipped,
    hintSquares,
  ];
}

/// Pure selection logic shared by games and lessons: turns taps into moves.
class BoardSelection {
  const new _();

  /// Result of tapping [square] with [selected] already chosen.
  static SelectionResult tap(
    GameState position,
    Square? selected,
    Square square,
  ) {
    if (selected != null) {
      final move = position
          .legalMovesFrom(selected)
          .where((m) => m.to == square)
          .firstOrNull;
      if (move != null) return SelectionResult.move(move);
      final piece = position.at(square);
      if (piece != null && piece.side != position.toMove) {
        return position.isSupplied(selected)
            ? const SelectionResult.rejected(noWater: false)
            : const SelectionResult.rejected(noWater: true);
      }
    }
    final piece = position.at(square);
    if (piece != null && piece.side == position.toMove && square != selected) {
      return SelectionResult.select(square);
    }
    return const SelectionResult.select(null);
  }
}

/// What a tap did.
class SelectionResult extends Equatable {
  const new move(Move this.move)
    : selected = null,
      rejected = false,
      noWater = false;

  const new select(this.selected)
    : move = null,
      rejected = false,
      noWater = false;

  const new rejected({required this.noWater})
    : move = null,
      selected = null,
      rejected = true;

  final Move? move;
  final Square? selected;
  final bool rejected;

  /// The capture failed because the selected piece has no water.
  final bool noWater;

  @override
  List<Object?> get props => [move, selected, rejected, noWater];
}
