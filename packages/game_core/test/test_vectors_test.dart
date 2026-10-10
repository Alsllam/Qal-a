import 'dart:convert';
import 'dart:io';

import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

void main() {
  test('committed test vectors match the engine', () {
    final file = File('test_vectors/rules_v${RuleSet.standard.version}.json');
    expect(file.existsSync(), isTrue,
        reason: 'run: dart run tool/export_vectors.dart');
    final committed = jsonDecode(file.readAsStringSync());
    final fresh = jsonDecode(jsonEncode(buildTestVectors()));
    expect(committed, fresh,
        reason: 'rules changed: run dart run tool/export_vectors.dart');
  });
}
