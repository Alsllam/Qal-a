import 'move.dart';
import 'outcome.dart';
import 'piece.dart';
import 'rule_set.dart';
import 'side.dart';
import 'square.dart';

/// Neighbour indices (8 directions) for every square, computed once.
final List<List<int>> _neighbours = List.generate(squareCount, (i) {
  final square = Square.fromIndex(i);
  return [
    for (final (df, dr) in allDirections)
      if (square.offset(df, dr) case final n?) n.index,
  ];
}, growable: false);

/// An immutable game position: the board, whose turn it is, the ply count
/// and, once the game is over, its [outcome].
///
/// Position notation: ranks 7 to 1 separated by `/`, upper case for South,
/// lower case for North, digits for runs of empty squares, then the side to
/// move (`s`/`n`) and the ply count. The opening position is
/// `1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0`. When the rule set uses water
/// points, a fourth field gives them as `south:north`, e.g. `… n 12 3:1`.
class GameState {
  GameState._(
    this.rules,
    this._cells,
    this.toMove,
    this.ply,
    this.outcome, [
    this._southWater = 0,
    this._northWater = 0,
  ]);

  /// The opening position for [rules].
  factory GameState.initial([RuleSet rules = RuleSet.standard]) =>
      GameState.fromNotation(
        '${rules.setup} s 0 0:${rules.northStartWater}',
        rules: rules,
      );

  /// Parses position notation. Side to move, ply count and water points are
  /// optional (defaults `s`, `0` and `0:0`). Each side must have exactly one
  /// Amir.
  factory GameState.fromNotation(
    String text, {
    RuleSet rules = RuleSet.standard,
  }) {
    final parts = text.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty || parts.length > 4) {
      throw FormatException('Bad position "$text"');
    }
    final ranks = parts[0].split('/');
    if (ranks.length != boardSize) {
      throw FormatException('Expected $boardSize ranks in "$text"');
    }
    final cells = List<Piece?>.filled(squareCount, null);
    for (var r = 0; r < boardSize; r++) {
      final rank = boardSize - 1 - r;
      var file = 0;
      for (final char in ranks[r].split('')) {
        final digit = int.tryParse(char);
        if (digit != null) {
          file += digit;
        } else {
          if (file >= boardSize) break;
          cells[Square(file, rank).index] = Piece.fromLetter(char);
          file++;
        }
      }
      if (file != boardSize) {
        throw FormatException('Rank ${rank + 1} does not have $boardSize '
            'squares in "$text"');
      }
    }
    for (final side in Side.values) {
      final amirs = cells.where((p) => p == Piece(PieceType.amir, side)).length;
      if (amirs != 1) {
        throw FormatException('${side.name} must have exactly one Amir');
      }
    }
    final toMove = parts.length > 1 ? Side.fromCode(parts[1]) : Side.south;
    final ply = parts.length > 2 ? int.parse(parts[2]) : 0;
    var water = const [0, 0];
    if (parts.length > 3) {
      water = parts[3].split(':').map(int.parse).toList();
      if (water.length != 2) throw FormatException('Bad water "${parts[3]}"');
    }
    return GameState._(
      rules,
      List.unmodifiable(cells),
      toMove,
      ply,
      null,
      water[0],
      water[1],
    );
  }

  final RuleSet rules;
  final List<Piece?> _cells;

  /// The side whose turn it is.
  final Side toMove;

  /// Number of plies (half-moves) played so far.
  final int ply;

  /// Null while the game is in progress.
  final Outcome? outcome;

  final int _southWater;
  final int _northWater;

  /// Water points of [side] (always 0 unless `RuleSet.waterToWin` is set).
  int water(Side side) => side == Side.south ? _southWater : _northWater;

  bool get isOver => outcome != null;

  Piece? at(Square square) => _cells[square.index];

  /// All pieces with their squares, optionally for one [side] only.
  Iterable<(Square, Piece)> pieces([Side? side]) sync* {
    for (var i = 0; i < squareCount; i++) {
      final piece = _cells[i];
      if (piece != null && (side == null || piece.side == side)) {
        yield (Square.fromIndex(i), piece);
      }
    }
  }

  int pieceCount(Side side) => _cells.where((p) => p?.side == side).length;

  /// Square of [side]'s Amir, or null if it has been captured.
  Square? amirSquare(Side side) {
    final index = _cells.indexOf(Piece(PieceType.amir, side));
    return index < 0 ? null : Square.fromIndex(index);
  }

  /// Number of Wells occupied by [side].
  int wellsHeld(Side side) =>
      rules.wells.where((w) => _cells[w.index]?.side == side).length;

  /// Wells that earn water points for [side] this turn.
  int _wellsScoring(Side side) => rules.wells
      .where((w) =>
          _cells[w.index]?.side == side &&
          (rules.amirEarnsWater || _cells[w.index]!.type != PieceType.amir) &&
          (!rules.waterNeedsSupply || isSupplied(w)))
      .length;

  late final List<bool> _southSupply = _computeSupply(Side.south);
  late final List<bool> _northSupply = _computeSupply(Side.north);

  List<bool> _supplyOf(Side side) =>
      side == Side.south ? _southSupply : _northSupply;

  /// Whether the piece on [square] is supplied. False for empty squares.
  bool isSupplied(Square square) {
    final piece = _cells[square.index];
    return piece != null && _supplyOf(piece.side)[square.index];
  }

  /// Squares of all supplied pieces of [side].
  Set<Square> suppliedSquares(Side side) {
    final mask = _supplyOf(side);
    return {
      for (var i = 0; i < squareCount; i++)
        if (mask[i]) Square.fromIndex(i),
    };
  }

  /// Flood fill from the water sources through touching friendly pieces.
  List<bool> _computeSupply(Side side) {
    final mask = List<bool>.filled(squareCount, false);
    final queue = <int>[];
    void seed(int i) {
      if (!mask[i] && _cells[i]?.side == side) {
        mask[i] = true;
        queue.add(i);
      }
    }

    final qala = rules.qalaOf(side).index;
    if (_cells[qala]?.side != side.opponent) {
      seed(qala);
      _neighbours[qala].forEach(seed);
    }
    if (rules.wellsAreSources) {
      for (final well in rules.wells) {
        seed(well.index);
      }
    }
    if (rules.amirIsSource) {
      final amir = _cells.indexOf(Piece(PieceType.amir, side));
      if (amir >= 0) seed(amir);
    }
    for (var head = 0; head < queue.length; head++) {
      _neighbours[queue[head]].forEach(seed);
    }
    return mask;
  }

  /// Every legal move for [toMove]. Empty once the game is over.
  late final List<Move> legalMoves = List.unmodifiable(
    isOver ? const <Move>[] : _generateMoves(),
  );

  /// Legal moves of the piece on [from].
  List<Move> legalMovesFrom(Square from) =>
      legalMoves.where((m) => m.from == from).toList();

  bool isLegal(Move move) => legalMoves.contains(move);

  List<Move> _generateMoves() {
    final moves = <Move>[];
    final supply = _supplyOf(toMove);
    for (var i = 0; i < squareCount; i++) {
      final piece = _cells[i];
      if (piece == null || piece.side != toMove) continue;
      final from = Square.fromIndex(i);
      final canCapture = supply[i];

      void slide(List<(int, int)> directions, int range) {
        for (final (df, dr) in directions) {
          for (var d = 1; d <= range; d++) {
            final to = from.offset(df * d, dr * d);
            if (to == null) break;
            final target = _cells[to.index];
            if (target == null) {
              moves.add(Move(from, to, MoveKind.step));
              continue;
            }
            if (target.side != toMove && canCapture) {
              moves.add(Move(from, to, MoveKind.capture));
            }
            break;
          }
        }
      }

      switch (piece.type) {
        case PieceType.amir:
          slide(allDirections, 1);
        case PieceType.jundi:
          slide(orthogonalDirections, 1);
        case PieceType.faris:
          slide(orthogonalDirections, rules.farisRange);
        case PieceType.rami:
          for (final (df, dr) in diagonalDirections) {
            final to = from.offset(df, dr);
            if (to != null && _cells[to.index] == null) {
              moves.add(Move(from, to, MoveKind.step));
            }
          }
          if (canCapture) _addShots(from, moves);
      }
    }
    return moves;
  }

  void _addShots(Square from, List<Move> moves) {
    final distance = rules.shotDistance;
    final directions =
        rules.diagonalShots ? allDirections : orthogonalDirections;
    for (final (df, dr) in directions) {
      final target = from.offset(df * distance, dr * distance);
      if (target == null || _cells[target.index]?.side != toMove.opponent) {
        continue;
      }
      var clear = true;
      for (var d = 1; d < distance && clear; d++) {
        clear = _cells[from.offset(df * d, dr * d)!.index] == null;
      }
      if (clear) moves.add(Move(from, target, MoveKind.shot));
    }
  }

  /// Plays a legal [move] and returns the new position.
  ///
  /// Throws [StateError] if the game is over and [ArgumentError] if the move
  /// is illegal.
  GameState play(Move move) {
    if (isOver) throw StateError('Game is over: $outcome');
    if (!isLegal(move)) {
      throw ArgumentError.value(move.notation, 'move', 'Illegal move');
    }
    return playUnchecked(move);
  }

  /// Plays [move] without checking legality. For search code that only
  /// plays moves taken from [legalMoves].
  GameState playUnchecked(Move move) {
    final cells = List<Piece?>.of(_cells);
    final mover = toMove;
    final removed = move.removesPiece ? cells[move.to.index] : null;
    switch (move.kind) {
      case MoveKind.step:
      case MoveKind.capture:
        cells[move.to.index] = cells[move.from.index];
        cells[move.from.index] = null;
      case MoveKind.shot:
        cells[move.to.index] = null;
    }
    final unmodifiable = List<Piece?>.unmodifiable(cells);
    var southWater = _southWater;
    var northWater = _northWater;
    GameState build(Outcome? outcome) => GameState._(rules, unmodifiable,
        mover.opponent, ply + 1, outcome, southWater, northWater);
    if (rules.waterToWin != null) {
      // The side about to move draws water from the Wells it still holds.
      final gained = build(null)._wellsScoring(mover.opponent);
      if (mover == Side.south) {
        northWater += gained;
      } else {
        southWater += gained;
      }
    }
    final next = build(null);
    final outcome = next._judge(mover, removed);
    return outcome == null ? next : build(outcome);
  }

  /// Decides whether the move just made by [mover] ended the game.
  Outcome? _judge(Side mover, Piece? removed) {
    if (removed?.type == PieceType.amir) {
      return Outcome(mover, EndReason.amirCaptured);
    }
    final enemyQala = rules.qalaOf(mover.opponent);
    if (at(enemyQala)?.side == mover && isSupplied(enemyQala)) {
      return Outcome(mover, EndReason.qalaTaken);
    }
    final waterToWin = rules.waterToWin;
    if (waterToWin != null && water(toMove) >= waterToWin) {
      return Outcome(toMove, EndReason.waterVictory);
    }
    if (ply >= rules.plyLimit) return _plyLimitOutcome();
    if (legalMoves.isEmpty) return Outcome(mover, EndReason.noLegalMoves);
    return null;
  }

  Outcome _plyLimitOutcome() {
    final waterLead = water(Side.south) - water(Side.north);
    if (waterLead != 0) {
      return Outcome(
          waterLead > 0 ? Side.south : Side.north, EndReason.plyLimitWater);
    }
    final wells = wellsHeld(Side.south) - wellsHeld(Side.north);
    if (wells != 0) {
      return Outcome(
          wells > 0 ? Side.south : Side.north, EndReason.plyLimitWells);
    }
    final material = pieceCount(Side.south) - pieceCount(Side.north);
    if (material != 0) {
      return Outcome(
        material > 0 ? Side.south : Side.north,
        EndReason.plyLimitMaterial,
      );
    }
    return const Outcome(null, EndReason.plyLimitDraw);
  }

  /// Position notation, e.g. `1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0`.
  String toNotation() {
    final ranks = <String>[];
    for (var rank = boardSize - 1; rank >= 0; rank--) {
      final buffer = StringBuffer();
      var empty = 0;
      for (var file = 0; file < boardSize; file++) {
        final piece = _cells[Square(file, rank).index];
        if (piece == null) {
          empty++;
          continue;
        }
        if (empty > 0) buffer.write(empty);
        empty = 0;
        buffer.write(piece.letter);
      }
      if (empty > 0) buffer.write(empty);
      ranks.add(buffer.toString());
    }
    final water = rules.waterToWin == null ? '' : ' $_southWater:$_northWater';
    return '${ranks.join('/')} ${toMove.code} $ply$water';
  }

  /// Text diagram. Empty Qal'a squares show `Q`, empty Wells `W`, and
  /// unsupplied pieces are marked with `'`.
  String toAscii() {
    final buffer = StringBuffer('    a  b  c  d  e  f  g\n');
    for (var rank = boardSize - 1; rank >= 0; rank--) {
      buffer.write('${rank + 1} |');
      for (var file = 0; file < boardSize; file++) {
        final square = Square(file, rank);
        final piece = at(square);
        final String cell;
        if (piece != null) {
          cell = '${piece.letter}${isSupplied(square) ? ' ' : "'"}';
        } else if (square == rules.southQala || square == rules.northQala) {
          cell = 'Q ';
        } else if (rules.wells.contains(square)) {
          cell = 'W ';
        } else {
          cell = '. ';
        }
        buffer.write(' $cell');
      }
      buffer.write('\n');
    }
    return buffer.toString();
  }

  @override
  bool operator ==(Object other) =>
      other is GameState &&
      other.toMove == toMove &&
      other.ply == ply &&
      other.toNotation() == toNotation();

  @override
  int get hashCode => toNotation().hashCode;

  @override
  String toString() => toNotation();
}
