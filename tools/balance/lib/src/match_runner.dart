import 'dart:isolate';
import 'dart:math';

import 'package:game_core/game_core.dart';

import 'players.dart';
import 'search.dart';
import 'variants.dart';

/// Recipe for a player, so it can be rebuilt inside a worker isolate.
class PlayerSpec {
  const PlayerSpec.alphaBeta({this.depth = 2, this.noise = 0.15})
      : random = false;

  const PlayerSpec.random()
      : depth = 0,
        noise = 0,
        random = true;

  final int depth;
  final double noise;
  final bool random;

  Player create() => random
      ? const RandomPlayer()
      : AlphaBetaPlayer(depth: depth, noise: noise);

  String get label => random ? 'random' : 'depth $depth';
}

/// A batch of games between player [a] and player [b].
class MatchSpec {
  const MatchSpec({
    required this.rulesVersion,
    required this.games,
    this.a = const PlayerSpec.alphaBeta(),
    this.b = const PlayerSpec.alphaBeta(),
    this.alternateSides = false,
    this.randomOpeningPlies = 2,
    this.seed = 1,
  });

  final String rulesVersion;
  final int games;
  final PlayerSpec a;
  final PlayerSpec b;

  /// If true, [a] plays South in even games and North in odd games.
  /// Otherwise [a] is always South.
  final bool alternateSides;

  /// Opening plies played at random, so that self-play explores many
  /// openings instead of repeating one game.
  final int randomOpeningPlies;
  final int seed;
}

/// Everything the report needs to know about one finished game.
class GameRecord {
  const GameRecord({
    required this.outcome,
    required this.moves,
    required this.movers,
    required this.victims,
    required this.moverWasSupplied,
    required this.unsuppliedShare,
    required this.aWasSouth,
  });

  final Outcome outcome;
  final List<Move> moves;

  /// Type of the moving piece, per ply.
  final List<PieceType> movers;

  /// Type of the removed piece per ply, or null for a step.
  final List<PieceType?> victims;

  /// Whether the moving piece was supplied before it moved, per ply.
  final List<bool> moverWasSupplied;

  /// Average share of non-Amir pieces (both sides) that were unsupplied.
  final double unsuppliedShare;

  final bool aWasSouth;

  int get plies => moves.length;

  /// Type of the piece that made the winning move, for wins decided on the
  /// board (not by the ply limit).
  PieceType? get winningPiece => switch (outcome.reason) {
        EndReason.amirCaptured ||
        EndReason.qalaTaken ||
        EndReason.noLegalMoves =>
          movers.last,
        _ => null,
      };
}

/// Plays one game.
GameRecord playGame({
  required Player south,
  required Player north,
  required RuleSet rules,
  required Random random,
  int randomOpeningPlies = 0,
  bool aWasSouth = true,
}) {
  var state = GameState.initial(rules);
  final moves = <Move>[];
  final movers = <PieceType>[];
  final victims = <PieceType?>[];
  final supplied = <bool>[];
  var unsuppliedSum = 0.0;
  const opener = RandomPlayer();

  while (!state.isOver) {
    final player = state.ply < randomOpeningPlies
        ? opener
        : (state.toMove == Side.south ? south : north);
    final move = player.choose(state, random);
    moves.add(move);
    movers.add(state.at(move.from)!.type);
    victims.add(move.removesPiece ? state.at(move.to)!.type : null);
    supplied.add(state.isSupplied(move.from));
    unsuppliedSum += _unsuppliedShare(state);
    state = state.playUnchecked(move);
  }
  return GameRecord(
    outcome: state.outcome!,
    moves: moves,
    movers: movers,
    victims: victims,
    moverWasSupplied: supplied,
    unsuppliedShare: unsuppliedSum / moves.length,
    aWasSouth: aWasSouth,
  );
}

double _unsuppliedShare(GameState state) {
  var total = 0;
  var unsupplied = 0;
  for (final (square, piece) in state.pieces()) {
    if (piece.type == PieceType.amir) continue;
    total++;
    if (!state.isSupplied(square)) unsupplied++;
  }
  return total == 0 ? 0 : unsupplied / total;
}

/// Plays game [index] of [spec]. Each game has its own seed, so results do
/// not depend on how games are split across workers.
GameRecord playMatchGame(MatchSpec spec, int index) {
  final rules = ruleVariants[spec.rulesVersion]!.rules;
  final random = Random(spec.seed * 1000003 + index);
  final aIsSouth = !spec.alternateSides || index.isEven;
  final a = spec.a.create();
  final b = spec.b.create();
  return playGame(
    south: aIsSouth ? a : b,
    north: aIsSouth ? b : a,
    rules: rules,
    random: random,
    randomOpeningPlies: spec.randomOpeningPlies,
    aWasSouth: aIsSouth,
  );
}

/// Plays all games of [spec] on [workers] isolates.
Future<List<GameRecord>> runMatch(
  MatchSpec spec, {
  int workers = 4,
  void Function(int done, int total)? onProgress,
}) async {
  if (!ruleVariants.containsKey(spec.rulesVersion)) {
    throw ArgumentError.value(spec.rulesVersion, 'rulesVersion', 'Unknown');
  }
  final chunk = max(1, min(25, spec.games ~/ (workers * 4)));
  final starts = [for (var s = 0; s < spec.games; s += chunk) s];
  final results = List<List<GameRecord>?>.filled(starts.length, null);
  var next = 0;
  var done = 0;

  Future<void> worker() async {
    while (next < starts.length) {
      final slot = next++;
      final start = starts[slot];
      final count = min(chunk, spec.games - start);
      results[slot] = await _runRange(spec, start, count);
      done += count;
      onProgress?.call(done, spec.games);
    }
  }

  await Future.wait([for (var w = 0; w < workers; w++) worker()]);
  return [for (final r in results) ...r!];
}

/// Kept top-level so the isolate closure captures only its arguments.
Future<List<GameRecord>> _runRange(MatchSpec spec, int start, int count) =>
    Isolate.run(
      () => [for (var i = 0; i < count; i++) playMatchGame(spec, start + i)],
    );
