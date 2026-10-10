import 'dart:convert';
import 'dart:io';

import 'package:game_core/game_core.dart';

/// Writes test_vectors/rules_v<version>.json for other rule implementations.
void main() {
  final vectors = buildTestVectors();
  final file = File('test_vectors/rules_v${RuleSet.standard.version}.json');
  file.writeAsStringSync(
      '${const JsonEncoder.withIndent(' ').convert(vectors)}\n');
  stdout.writeln('Wrote ${file.path}: '
      '${(vectors['positions']! as List).length} positions');
}
