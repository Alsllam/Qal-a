import 'square.dart';

/// How a move affects the board.
enum MoveKind {
  /// Move to an empty square.
  step('-'),

  /// Move onto an enemy piece and remove it.
  capture('x'),

  /// Rami shot: remove the enemy on [Move.to]; the archer stays on [Move.from].
  shot('*');

  const MoveKind(this.symbol);

  final String symbol;

  static MoveKind fromSymbol(String symbol) => MoveKind.values.firstWhere(
        (k) => k.symbol == symbol,
        orElse: () => throw FormatException('Unknown move symbol "$symbol"'),
      );
}

/// A single move. Written as `b1-b4` (step), `c3xc4` (capture) or `d2*d4` (shot).
class Move {
  const Move(this.from, this.to, this.kind);

  factory Move.parse(String text) {
    final match = RegExp(r'^([a-g][1-7])([-x*])([a-g][1-7])$').firstMatch(text);
    if (match == null) throw FormatException('Bad move "$text"');
    return Move(
      Square.parse(match[1]!),
      Square.parse(match[3]!),
      MoveKind.fromSymbol(match[2]!),
    );
  }

  final Square from;
  final Square to;
  final MoveKind kind;

  bool get removesPiece => kind != MoveKind.step;

  String get notation => '${from.name}${kind.symbol}${to.name}';

  @override
  bool operator ==(Object other) =>
      other is Move &&
      other.from == from &&
      other.to == to &&
      other.kind == kind;

  @override
  int get hashCode => Object.hash(from, to, kind);

  @override
  String toString() => notation;
}
