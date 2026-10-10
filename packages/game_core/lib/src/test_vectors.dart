import 'dart:math';

import 'game_state.dart';
import 'move.dart';
import 'rule_set.dart';
import 'side.dart';

/// Builds the cross-language test vectors for [rules]: positions from seeded
/// random games with their legal moves, supply and the result of one move.
///
/// Other implementations of the rules (the C# server port) must reproduce
/// every field. Regenerate with `dart run tool/export_vectors.dart`.
Map<String, Object?> buildTestVectors({
  RuleSet rules = RuleSet.standard,
  int games = 40,
  int seed = 2026,
}) {
  final random = Random(seed);
  final positions = <Map<String, Object?>>[];
  for (var g = 0; g < games; g++) {
    var state = GameState.initial(rules);
    while (!state.isOver) {
      final moves = state.legalMoves;
      final move = moves[random.nextInt(moves.length)];
      final next = state.play(move);
      positions.add({
        'position': state.toNotation(),
        'legalMoves': [for (final m in moves) m.notation]..sort(),
        'suppliedSouth': _names(state, Side.south),
        'suppliedNorth': _names(state, Side.north),
        'move': move.notation,
        'after': next.toNotation(),
        'outcome': next.outcome == null
            ? null
            : {
                'winner': next.outcome!.winner?.name,
                'reason': next.outcome!.reason.name,
              },
      });
      state = next;
    }
  }
  return {
    'rulesVersion': rules.version,
    'rules': {
      'plyLimit': rules.plyLimit,
      'farisRange': rules.farisRange,
      'shotDistance': rules.shotDistance,
      'amirIsSource': rules.amirIsSource,
      'wells': [for (final w in rules.wells) w.name],
      'southQala': rules.southQala.name,
      'northQala': rules.northQala.name,
      'setup': rules.setup,
      'waterToWin': rules.waterToWin,
      'wellsAreSources': rules.wellsAreSources,
      'waterNeedsSupply': rules.waterNeedsSupply,
      'northStartWater': rules.northStartWater,
      'amirEarnsWater': rules.amirEarnsWater,
      'diagonalShots': rules.diagonalShots,
    },
    'perft': [
      for (var d = 1; d <= 3; d++) _perft(GameState.initial(rules), d),
    ],
    'positions': positions,
  };
}

List<String> _names(GameState state, Side side) =>
    [for (final s in state.suppliedSquares(side)) s.name]..sort();

int _perft(GameState state, int depth) {
  if (depth == 0 || state.isOver) return 1;
  var total = 0;
  for (final Move move in state.legalMoves) {
    total += _perft(state.playUnchecked(move), depth - 1);
  }
  return total;
}
