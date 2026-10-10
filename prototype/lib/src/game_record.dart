import 'package:game_core/game_core.dart';

import 'match_controller.dart';

/// One played move with who played it and how long it took.
class MoveLogEntry {
  const MoveLogEntry({
    required this.move,
    required this.side,
    required this.piece,
    required this.elapsed,
  });

  final Move move;
  final Side side;
  final PieceType piece;
  final Duration elapsed;
}

/// Text record of a match for the playtest log: a readable summary, a CSV
/// row matching docs/playtest-guide.md §8, and the full move list.
class GameRecordText {
  GameRecordText({
    required this.config,
    required this.log,
    required this.finalState,
    required this.startedAt,
    required this.endedAt,
    required this.illegalCaptureAttempts,
    required this.undos,
  });

  final MatchConfig config;
  final List<MoveLogEntry> log;
  final GameState finalState;
  final DateTime startedAt;
  final DateTime endedAt;
  final int illegalCaptureAttempts;
  final int undos;

  String get mode => config.vsAi
      ? 'ai-${config.aiLevel!.name} (human ${config.humanSide.name})'
      : 'pvp';

  int get ramiMoves => log.where((e) => e.piece == PieceType.rami).length;
  int get ramiShots => log.where((e) => e.move.kind == MoveKind.shot).length;

  MoveLogEntry? get slowest =>
      log.isEmpty ? null : log.reduce((a, b) => a.elapsed >= b.elapsed ? a : b);

  Duration get playTime => log.fold(Duration.zero, (sum, e) => sum + e.elapsed);

  String get resultText {
    final outcome = finalState.outcome;
    if (outcome == null) return 'unfinished';
    return outcome.isDraw ? 'draw' : '${outcome.winner!.name} wins';
  }

  String get endReason => finalState.outcome?.reason.name ?? '-';

  /// Row for the session spreadsheet.
  String get csv => [
    config.rules.version,
    config.vsAi ? 'ai-${config.aiLevel!.name}' : 'pvp',
    finalState.outcome?.winner?.name ?? (finalState.isOver ? 'draw' : '-'),
    endReason,
    log.length,
    (playTime.inSeconds / 60).toStringAsFixed(1),
    slowest?.elapsed.inSeconds ?? 0,
    log.isEmpty ? '-' : log.first.move.notation,
    ramiMoves,
    ramiShots,
    illegalCaptureAttempts,
  ].join(',');

  @override
  String toString() {
    final b = StringBuffer()
      ..writeln("Qal'a playtest record")
      ..writeln(
        'rules: ${config.rules.version} | mode: $mode | '
        'started: ${_stamp(startedAt)}',
      )
      ..writeln(
        'result: $resultText ($endReason) after ${log.length} plies, '
        '${_duration(playTime)}',
      )
      ..writeln(
        'water: south ${finalState.water(Side.south)} / '
        'north ${finalState.water(Side.north)}',
      )
      ..writeln(
        'first move: ${log.isEmpty ? '-' : log.first.move.notation} | '
        'rami: $ramiMoves moves, $ramiShots shots | '
        'illegal capture attempts: $illegalCaptureAttempts | undos: $undos',
      );
    final slow = slowest;
    if (slow != null) {
      b.writeln(
        'slowest move: ${slow.elapsed.inSeconds}s '
        '(ply ${log.indexOf(slow) + 1}, ${slow.side.name})',
      );
    }
    b
      ..writeln(
        'csv: rules_version,mode,winner,end_reason,plies,minutes,'
        'slowest_move_s,first_move,rami_moves,rami_shots,'
        'illegal_capture_attempts',
      )
      ..writeln('csv: $csv')
      ..writeln('moves:');
    for (var i = 0; i < log.length; i += 2) {
      final line = StringBuffer('${i ~/ 2 + 1}. ${_entry(log[i])}');
      if (i + 1 < log.length) line.write('   ${_entry(log[i + 1])}');
      b.writeln(line);
    }
    b.writeln('final position: ${finalState.toNotation()}');
    return b.toString();
  }

  static String _entry(MoveLogEntry e) =>
      '${e.piece.letter}${e.move.notation} (${e.elapsed.inSeconds}s)';

  static String _duration(Duration d) =>
      '${d.inMinutes}m ${(d.inSeconds % 60).toString().padLeft(2, '0')}s';

  static String _stamp(DateTime t) =>
      '${t.year}-${_two(t.month)}-${_two(t.day)} ${_two(t.hour)}:${_two(t.minute)}';

  static String _two(int n) => n.toString().padLeft(2, '0');
}
