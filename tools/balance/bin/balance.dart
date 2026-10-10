import 'dart:io';

import 'package:args/args.dart';
import 'package:balance/balance.dart';

/// Usage: dart run bin/balance.dart --rules 0.1 --games 2000
Future<void> main(List<String> arguments) async {
  final parser = ArgParser()
    ..addOption('rules',
        defaultsTo: ruleVariants.keys.last,
        help: 'Rules version (${ruleVariants.keys.join(', ')}) or "all"')
    ..addOption('games', defaultsTo: '2000', help: 'Self-play games')
    ..addOption('depth', defaultsTo: '2', help: 'Self-play search depth')
    ..addOption('noise', defaultsTo: '0.15', help: 'Move choice noise')
    ..addOption('openings', defaultsTo: '2', help: 'Random opening plies')
    ..addOption('gradient',
        defaultsTo: '200', help: 'Games of depth+1 vs depth-1 (0 to skip)')
    ..addOption('workers', defaultsTo: '${Platform.numberOfProcessors}')
    ..addOption('seed', defaultsTo: '1')
    ..addOption('out', defaultsTo: 'reports', help: 'Report directory')
    ..addFlag('help', abbr: 'h', negatable: false);
  final args = parser.parse(arguments);
  if (args.flag('help')) {
    stdout.writeln(parser.usage);
    return;
  }

  final versions = args.option('rules') == 'all'
      ? ruleVariants.keys.toList()
      : [args.option('rules')!];
  final games = int.parse(args.option('games')!);
  final depth = int.parse(args.option('depth')!);
  final noise = double.parse(args.option('noise')!);
  final openings = int.parse(args.option('openings')!);
  final gradientGames = int.parse(args.option('gradient')!);
  final workers = int.parse(args.option('workers')!);
  final seed = int.parse(args.option('seed')!);
  final outDir = Directory(args.option('out')!)..createSync(recursive: true);

  for (final version in versions) {
    final variant = ruleVariants[version];
    if (variant == null) {
      stderr.writeln('Unknown rules version $version');
      exitCode = 64;
      return;
    }
    final watch = Stopwatch()..start();
    final player = PlayerSpec.alphaBeta(depth: depth, noise: noise);
    final spec = MatchSpec(
      rulesVersion: version,
      games: games,
      a: player,
      b: player,
      randomOpeningPlies: openings,
      seed: seed,
    );
    stdout.writeln('v$version: $games self-play games at depth $depth…');
    final main = MatchStats(await runMatch(spec, workers: workers));

    MatchSpec? gradientSpec;
    MatchStats? gradient;
    if (gradientGames > 0) {
      gradientSpec = MatchSpec(
        rulesVersion: version,
        games: gradientGames,
        a: PlayerSpec.alphaBeta(depth: depth + 1, noise: noise),
        b: PlayerSpec.alphaBeta(
            depth: depth - 1 < 1 ? 1 : depth - 1, noise: noise),
        alternateSides: true,
        randomOpeningPlies: openings,
        seed: seed + 7,
      );
      stdout.writeln('v$version: $gradientGames skill-gradient games…');
      gradient = MatchStats(await runMatch(gradientSpec, workers: workers));
    }
    watch.stop();

    final report = buildReport(
      variant: variant,
      spec: spec,
      main: main,
      gradientSpec: gradientSpec,
      gradient: gradient,
      elapsed: watch.elapsed,
    );
    final file = File('${outDir.path}/v$version.md')..writeAsStringSync(report);
    for (final check in runChecks(main, gradient: gradient)) {
      stdout.writeln('  ${check.passed ? 'PASS' : 'FAIL'}  ${check.name}: '
          '${check.detail}');
    }
    stdout.writeln('  report: ${file.path} (${watch.elapsed.inSeconds}s)');
  }
}
