import 'side.dart';

/// The four piece types.
enum PieceType {
  /// Leader. One step in any direction. Losing it loses the game.
  amir('A', 'Amir', 'أمير'),

  /// Soldier. One step orthogonally.
  jundi('J', 'Jundi', 'جندي'),

  /// Horseman. Slides up to `RuleSet.farisRange` squares orthogonally.
  faris('F', 'Faris', 'فارس'),

  /// Archer. Steps one square diagonally; shoots instead of capturing by moving.
  rami('R', 'Rami', 'رامي');

  const PieceType(this.letter, this.englishName, this.arabicName);

  /// Upper-case letter used in notation.
  final String letter;
  final String englishName;
  final String arabicName;

  static PieceType fromLetter(String letter) => PieceType.values.firstWhere(
        (t) => t.letter == letter.toUpperCase(),
        orElse: () => throw FormatException('Unknown piece "$letter"'),
      );
}

/// A piece: a type and an owner.
class Piece {
  const Piece(this.type, this.side);

  /// Parses a notation letter: upper case is South, lower case is North.
  factory Piece.fromLetter(String letter) {
    final side = letter == letter.toUpperCase() ? Side.south : Side.north;
    return Piece(PieceType.fromLetter(letter), side);
  }

  final PieceType type;
  final Side side;

  /// Notation letter: upper case for South, lower case for North.
  String get letter =>
      side == Side.south ? type.letter : type.letter.toLowerCase();

  @override
  bool operator ==(Object other) =>
      other is Piece && other.type == type && other.side == side;

  @override
  int get hashCode => Object.hash(type, side);

  @override
  String toString() => letter;
}
