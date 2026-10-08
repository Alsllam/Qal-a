/// Board width and height.
const int boardSize = 7;

/// Number of squares on the board.
const int squareCount = boardSize * boardSize;

/// A square on the 7×7 board. Files `a–g` map to 0–6, ranks `1–7` to 0–6.
/// Rank 1 is South's home rank.
class Square implements Comparable<Square> {
  const Square(this.file, this.rank)
      : assert(file >= 0 && file < boardSize),
        assert(rank >= 0 && rank < boardSize);

  factory Square.fromIndex(int index) {
    RangeError.checkValidIndex(index, null, 'index', squareCount);
    return Square(index % boardSize, index ~/ boardSize);
  }

  /// Parses algebraic names such as `d4`.
  factory Square.parse(String name) {
    if (name.length != 2) throw FormatException('Bad square "$name"');
    final file = name.codeUnitAt(0) - 0x61; // 'a'
    final rank = name.codeUnitAt(1) - 0x31; // '1'
    if (!isOnBoard(file, rank)) throw FormatException('Bad square "$name"');
    return Square(file, rank);
  }

  final int file;
  final int rank;

  int get index => rank * boardSize + file;

  String get name => '${String.fromCharCode(0x61 + file)}${rank + 1}';

  /// The square [df] files and [dr] ranks away, or null if off the board.
  Square? offset(int df, int dr) {
    final f = file + df;
    final r = rank + dr;
    return isOnBoard(f, r) ? Square(f, r) : null;
  }

  static bool isOnBoard(int file, int rank) =>
      file >= 0 && file < boardSize && rank >= 0 && rank < boardSize;

  @override
  bool operator ==(Object other) =>
      other is Square && other.file == file && other.rank == rank;

  @override
  int get hashCode => index;

  @override
  int compareTo(Square other) => index.compareTo(other.index);

  @override
  String toString() => name;
}

/// The four orthogonal directions as (file, rank) deltas.
const List<(int, int)> orthogonalDirections = [
  (0, 1),
  (1, 0),
  (0, -1),
  (-1, 0)
];

/// The four diagonal directions as (file, rank) deltas.
const List<(int, int)> diagonalDirections = [
  (1, 1),
  (1, -1),
  (-1, -1),
  (-1, 1)
];

/// All eight directions.
const List<(int, int)> allDirections = [
  ...orthogonalDirections,
  ...diagonalDirections,
];
