import 'package:game_core/game_core.dart';

import 'match_runner.dart';
import 'stats.dart';
import 'variants.dart';

/// Balance targets. A rules version is "balanced" when every check passes.
class BalanceTargets {
  const BalanceTargets();

  /// South's score must be within this distance of 50%.
  final double firstPlayerTolerance = 0.05;
  final double maxDrawRate = 0.10;
  final double minMeanPlies = 20;
  final double maxMeanPlies = 50;

  /// No single end reason may decide more than this share of games.
  final double maxEndReasonShare = 0.75;

  /// No piece type may make more than this share of moves...
  final double maxPieceMoveShare = 0.45;

  /// ...and every type must make at least this share.
  final double minPieceMoveShare = 0.08;

  /// The supply rule must matter: unsupplied pieces at least this often.
  final double minUnsuppliedShare = 0.05;

  /// Deeper search must beat shallower search at least this often.
  final double minSkillGradient = 0.70;

  /// A first move whose South score exceeds the overall score by more than
  /// this (with enough games) is flagged as a dominant opening.
  final double maxOpeningEdge = 0.15;
  final int minOpeningGames = 40;
}

/// One pass/fail line of the report.
class Check {
  const Check(this.name, this.passed, this.detail);

  final String name;
  final bool passed;
  final String detail;

  String get markdown => '| ${passed ? '✅' : '⚠️'} | $name | $detail |';
}

String _pct(double x) => '${(x * 100).toStringAsFixed(1)}%';

/// Runs every balance check over a self-play [main] match and an optional
/// skill-gradient match.
List<Check> runChecks(
  MatchStats main, {
  MatchStats? gradient,
  BalanceTargets targets = const BalanceTargets(),
}) {
  final checks = <Check>[];
  final edge = (main.southScore - 0.5).abs();
  checks.add(Check(
    'First-player balance',
    edge <= targets.firstPlayerTolerance,
    'South scores ${_pct(main.southScore)} ± ${_pct(main.southScoreMargin)} '
        '(target 50% ± ${_pct(targets.firstPlayerTolerance)})',
  ));
  checks.add(Check(
    'Few draws',
    main.drawRate <= targets.maxDrawRate,
    '${_pct(main.drawRate)} draws (target ≤ ${_pct(targets.maxDrawRate)})',
  ));
  checks.add(Check(
    'Game length',
    main.meanLength >= targets.minMeanPlies &&
        main.meanLength <= targets.maxMeanPlies,
    'mean ${main.meanLength.toStringAsFixed(1)} plies '
        '(target ${targets.minMeanPlies.toInt()}–${targets.maxMeanPlies.toInt()})',
  ));
  final topReason =
      main.endReasons.entries.reduce((a, b) => a.value >= b.value ? a : b);
  checks.add(Check(
    'Varied endings',
    topReason.value / main.games <= targets.maxEndReasonShare,
    '${topReason.key.name} ends ${_pct(topReason.value / main.games)} of games '
        '(target ≤ ${_pct(targets.maxEndReasonShare)})',
  ));
  final shares = {
    for (final MapEntry(:key, :value) in main.movesByType.entries)
      key: value / main.totalMoves,
  };
  final most = shares.entries.reduce((a, b) => a.value >= b.value ? a : b);
  final least = shares.entries.reduce((a, b) => a.value <= b.value ? a : b);
  checks.add(Check(
    'Every piece matters',
    most.value <= targets.maxPieceMoveShare &&
        least.value >= targets.minPieceMoveShare,
    'most used ${most.key.englishName} ${_pct(most.value)}, least used '
        '${least.key.englishName} ${_pct(least.value)} (targets ≤ '
        '${_pct(targets.maxPieceMoveShare)} and ≥ ${_pct(targets.minPieceMoveShare)})',
  ));
  checks.add(Check(
    'Supply matters',
    main.unsuppliedPieceShare >= targets.minUnsuppliedShare,
    '${_pct(main.unsuppliedPieceShare)} of pieces unsupplied on average '
        '(target ≥ ${_pct(targets.minUnsuppliedShare)})',
  ));
  final dominant = main.byFirstMove.entries
      .where((e) =>
          e.value.$1 >= targets.minOpeningGames &&
          e.value.$2 - main.southScore > targets.maxOpeningEdge)
      .map((e) => '${e.key} (${_pct(e.value.$2)})')
      .toList();
  checks.add(Check(
    'No dominant opening',
    dominant.isEmpty,
    dominant.isEmpty
        ? 'no first move beats the average by more than '
            '${_pct(targets.maxOpeningEdge)}'
        : 'dominant: ${dominant.join(', ')}',
  ));
  if (gradient != null) {
    checks.add(Check(
      'Skill is rewarded',
      gradient.aScore >= targets.minSkillGradient,
      'deeper AI scores ${_pct(gradient.aScore)} against shallower '
          '(target ≥ ${_pct(targets.minSkillGradient)})',
    ));
  }
  return checks;
}

/// Markdown report for one rules version.
String buildReport({
  required RuleVariant variant,
  required MatchSpec spec,
  required MatchStats main,
  MatchSpec? gradientSpec,
  MatchStats? gradient,
  Duration? elapsed,
}) {
  final checks = runChecks(main, gradient: gradient);
  final passed = checks.where((c) => c.passed).length;
  final b = StringBuffer()
    ..writeln('# Balance report: rules v${variant.version}')
    ..writeln()
    ..writeln('> ${variant.summary}')
    ..writeln()
    ..writeln('- Self-play: ${spec.games} games, ${spec.a.label} vs '
        '${spec.b.label}, noise ${spec.a.noise}, '
        '${spec.randomOpeningPlies} random opening plies, seed ${spec.seed}')
    ..writeln(gradientSpec == null
        ? '- Skill gradient: not run'
        : '- Skill gradient: ${gradientSpec.games} games, '
            '${gradientSpec.a.label} vs ${gradientSpec.b.label}, sides alternate');
  if (elapsed != null) b.writeln('- Run time: ${elapsed.inSeconds}s');
  b
    ..writeln()
    ..writeln('## Verdict: $passed / ${checks.length} checks pass')
    ..writeln()
    ..writeln('| | Check | Result |')
    ..writeln('|---|---|---|');
  for (final c in checks) {
    b.writeln(c.markdown);
  }

  b
    ..writeln()
    ..writeln('## Results')
    ..writeln()
    ..writeln('| South wins | North wins | Draws | South score |')
    ..writeln('|---|---|---|---|')
    ..writeln('| ${main.southWins} (${_pct(main.southWins / main.games)}) | '
        '${main.northWins} (${_pct(main.northWins / main.games)}) | '
        '${main.draws} (${_pct(main.drawRate)}) | '
        '${_pct(main.southScore)} ± ${_pct(main.southScoreMargin)} |')
    ..writeln()
    ..writeln(
        '**Game length (plies):** mean ${main.meanLength.toStringAsFixed(1)}, '
        'p10 ${main.percentileLength(0.1)}, median ${main.percentileLength(0.5)}, '
        'p90 ${main.percentileLength(0.9)}')
    ..writeln()
    ..writeln('### How games end')
    ..writeln()
    ..writeln('| Reason | Games | Share |')
    ..writeln('|---|---|---|');
  for (final MapEntry(:key, :value) in main.endReasons.entries) {
    if (value == 0) continue;
    b.writeln('| ${key.name} | $value | ${_pct(value / main.games)} |');
  }

  b
    ..writeln()
    ..writeln('### Piece usage')
    ..writeln()
    ..writeln('| Piece | Moves | Share of moves | Captures made | Lost | '
        'Delivered the win |')
    ..writeln('|---|---|---|---|---|---|');
  for (final type in PieceType.values) {
    b.writeln('| ${type.englishName} | ${main.movesByType[type]} | '
        '${_pct(main.movesByType[type]! / main.totalMoves)} | '
        '${main.capturesByType[type]} | ${main.lostByType[type]} | '
        '${main.winningPieces[type]} |');
  }
  b
    ..writeln()
    ..writeln('- Captures per game: '
        '${(main.totalCaptures / main.games).toStringAsFixed(2)}')
    ..writeln('- Moves made by unsupplied pieces: '
        '${_pct(main.unsuppliedMoveShare)}')
    ..writeln('- Average share of unsupplied pieces: '
        '${_pct(main.unsuppliedPieceShare)}');

  final openings = main.byFirstMove.entries.toList()
    ..sort((x, y) => y.value.$1.compareTo(x.value.$1));
  b
    ..writeln()
    ..writeln('### First move (random opening) → South score')
    ..writeln()
    ..writeln('| First move | Games | South score |')
    ..writeln('|---|---|---|');
  for (final MapEntry(:key, :value) in openings) {
    b.writeln('| $key | ${value.$1} | ${_pct(value.$2)} |');
  }

  if (gradient != null) {
    b
      ..writeln()
      ..writeln('### Skill gradient')
      ..writeln()
      ..writeln('${gradientSpec!.a.label} scored ${_pct(gradient.aScore)} '
          'against ${gradientSpec.b.label} over ${gradient.games} games.');
  }
  return b.toString();
}
