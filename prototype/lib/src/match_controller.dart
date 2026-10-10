import 'dart:async';
import 'dart:math';

import 'package:flutter/foundation.dart';
import 'package:game_ai/game_ai.dart';
import 'package:game_core/game_core.dart';

import 'game_record.dart';
import 'strings.dart';

export 'game_record.dart';

/// How a match is set up.
class MatchConfig {
  const MatchConfig({
    this.rules = RuleSet.standard,
    this.aiLevel,
    this.humanSide = Side.south,
    this.startPosition,
  });

  final RuleSet rules;

  /// Null for pass-and-play.
  final AiLevel? aiLevel;

  /// The human's side when playing the AI.
  final Side humanSide;

  /// Optional start position in position notation (tests, teaching puzzles).
  final String? startPosition;

  bool get vsAi => aiLevel != null;

  Side? get aiSide => vsAi ? humanSide.opponent : null;
}

/// A message shown briefly to the player.
enum Hint { noWater, notAllowed }

/// Chooses a move for the AI. Replaceable in tests.
typedef AiMoveFinder = Future<Move> Function(
  GameState state,
  AiLevel level,
  int seed,
);

/// Default AI: runs the search off the UI thread where the platform allows.
Future<Move> computeAiMove(GameState state, AiLevel level, int seed) =>
    compute(_searchMove, (state, level.depth, level.noise, seed));

Move _searchMove((GameState, int, double, int) request) {
  final (state, depth, noise, seed) = request;
  return AlphaBetaPlayer(
    depth: depth,
    noise: noise,
  ).choose(state, Random(seed));
}

/// The state of one match: position, history, selection, AI turns and the
/// playtest record. The UI only reads it and calls [tap], [undo], [restart].
class MatchController extends ChangeNotifier {
  MatchController(
    this.config, {
    AiMoveFinder? findAiMove,
    this.aiMinDelay = const Duration(milliseconds: 450),
    DateTime Function()? clock,
  }) : _findAiMove = findAiMove ?? computeAiMove,
       _clock = clock ?? DateTime.now {
    _reset();
  }

  final MatchConfig config;
  final AiMoveFinder _findAiMove;
  final Duration aiMinDelay;
  final DateTime Function() _clock;

  late GameState _state;
  final List<GameState> _history = [];
  final List<MoveLogEntry> _log = [];
  late DateTime _startedAt;
  late DateTime _turnStartedAt;
  Square? _selected;
  bool _aiThinking = false;
  int _aiGeneration = 0;
  int _illegalCaptureAttempts = 0;
  int _undos = 0;
  final _hints = StreamController<Hint>.broadcast();

  GameState get state => _state;
  Square? get selected => _selected;
  bool get aiThinking => _aiThinking;
  List<MoveLogEntry> get log => List.unmodifiable(_log);
  Move? get lastMove => _log.isEmpty ? null : _log.last.move;

  /// Short messages for the player (e.g. "no water").
  Stream<Hint> get hints => _hints.stream;

  /// Legal moves of the selected piece.
  List<Move> get selectedMoves =>
      _selected == null ? const [] : _state.legalMovesFrom(_selected!);

  bool get isHumanTurn =>
      !_state.isOver && (!config.vsAi || _state.toMove == config.humanSide);

  bool get canUndo => !_aiThinking && _humanPliesToUndo() > 0;

  void _reset() {
    final start = config.startPosition;
    _state = start == null
        ? GameState.initial(config.rules)
        : GameState.fromNotation(start, rules: config.rules);
    _history.clear();
    _log.clear();
    _selected = null;
    _illegalCaptureAttempts = 0;
    _undos = 0;
    _startedAt = _clock();
    _turnStartedAt = _startedAt;
    _aiGeneration++;
    _aiThinking = false;
    _maybeStartAi();
  }

  void restart() {
    _reset();
    notifyListeners();
  }

  /// Handles a tap on [square].
  void tap(Square square) {
    if (!isHumanTurn || _aiThinking) return;
    final piece = _state.at(square);
    final from = _selected;

    if (from != null) {
      final move = selectedMoves.where((m) => m.to == square).firstOrNull;
      if (move != null) {
        _play(move);
        return;
      }
      if (piece != null && piece.side != _state.toMove) {
        // Tapped an enemy that the selected piece cannot take.
        if (!_state.isSupplied(from)) {
          _illegalCaptureAttempts++;
          _hints.add(Hint.noWater);
        } else {
          _hints.add(Hint.notAllowed);
        }
        return;
      }
    }

    if (piece != null && piece.side == _state.toMove) {
      _selected = square == from ? null : square;
    } else {
      _selected = null;
    }
    notifyListeners();
  }

  void _play(Move move) {
    final now = _clock();
    final piece = _state.at(move.from)!;
    _log.add(
      MoveLogEntry(
        move: move,
        side: piece.side,
        piece: piece.type,
        elapsed: now.difference(_turnStartedAt),
      ),
    );
    _history.add(_state);
    _state = _state.play(move);
    _turnStartedAt = now;
    _selected = null;
    notifyListeners();
    _maybeStartAi();
  }

  void _maybeStartAi() {
    final level = config.aiLevel;
    if (level == null || _state.isOver || _state.toMove != config.aiSide) {
      return;
    }
    _aiThinking = true;
    final generation = _aiGeneration;
    final position = _state;
    final started = _clock();
    unawaited(() async {
      final move = await _findAiMove(position, level, _log.length + 1);
      final wait = aiMinDelay - _clock().difference(started);
      if (wait > Duration.zero) await Future<void>.delayed(wait);
      if (generation != _aiGeneration || !identical(position, _state)) return;
      _aiThinking = false;
      _play(move);
    }());
    notifyListeners();
  }

  int _humanPliesToUndo() {
    if (_history.isEmpty) return 0;
    if (!config.vsAi) return 1;
    // Undo back to the human's previous turn.
    var count = 0;
    for (var i = _log.length - 1; i >= 0; i--) {
      count++;
      if (_log[i].side == config.humanSide) return count;
    }
    return 0;
  }

  void undo() {
    final plies = _humanPliesToUndo();
    if (plies == 0 || _aiThinking) return;
    for (var i = 0; i < plies; i++) {
      _state = _history.removeLast();
      _log.removeLast();
    }
    _undos++;
    _selected = null;
    _turnStartedAt = _clock();
    notifyListeners();
  }

  /// Playtest record of the match so far (see docs/playtest-guide.md).
  GameRecordText record() => GameRecordText(
    config: config,
    log: _log,
    finalState: _state,
    startedAt: _startedAt,
    endedAt: _clock(),
    illegalCaptureAttempts: _illegalCaptureAttempts,
    undos: _undos,
  );

  @override
  void dispose() {
    _aiGeneration++;
    _hints.close();
    super.dispose();
  }
}
