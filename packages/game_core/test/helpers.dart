import 'package:game_core/game_core.dart';

/// Builds a position from `{'d1': 'A', 'd7': 'a'}` (upper case South,
/// lower case North).
GameState position(
  Map<String, String> pieces, {
  Side toMove = Side.south,
  int ply = 0,
  RuleSet rules = RuleSet.standard,
}) {
  final grid = List.generate(boardSize, (_) => List.filled(boardSize, '.'));
  pieces.forEach((name, letter) {
    final square = Square.parse(name);
    grid[square.rank][square.file] = letter;
  });
  final ranks = [
    for (var rank = boardSize - 1; rank >= 0; rank--)
      grid[rank]
          .join()
          .replaceAllMapped(RegExp(r'\.+'), (m) => '${m[0]!.length}'),
  ];
  return GameState.fromNotation(
    '${ranks.join('/')} ${toMove.code} $ply',
    rules: rules,
  );
}

/// Legal moves in notation, optionally only those from [from].
Set<String> moves(GameState state, [String? from]) => {
      for (final m in state.legalMoves)
        if (from == null || m.from.name == from) m.notation,
    };

/// Square names of [side]'s supplied pieces.
Set<String> supplied(GameState state, Side side) =>
    {for (final s in state.suppliedSquares(side)) s.name};
