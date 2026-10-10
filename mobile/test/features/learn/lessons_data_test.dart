import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:game_core/game_core.dart';
import 'package:qala/features/learn/data/datasources/lesson_asset_data_source.dart';

void main() {
  final chapters = LessonAssetDataSource.parse(
    File('assets/lessons/lessons.json').readAsStringSync(),
  );
  final lessons = chapters.expand((c) => c.lessons).toList();

  test('lessons exist and ids are unique', () {
    expect(chapters, isNotEmpty);
    expect(lessons.map((l) => l.id).toSet(), hasLength(lessons.length));
  });

  for (final lesson in lessons) {
    test('${lesson.id}: every step is playable', () {
      for (final step in lesson.steps) {
        final position = GameState.fromNotation(step.position);
        expect(position.isOver, isFalse);
        expect(step.text.ar, isNotEmpty);
        expect(step.text.en, isNotEmpty);
        for (final expected in step.expected) {
          expect(
            position.isLegal(Move.parse(expected)),
            isTrue,
            reason: '$expected must be legal in ${step.position}',
          );
        }
      }
    });
  }

  test('the supply lesson really reconnects the Faris', () {
    final step = lessons
        .firstWhere((l) => l.id == 'c2-supply')
        .steps
        .firstWhere((s) => s.expected.isNotEmpty);
    final position = GameState.fromNotation(step.position);
    final faris = Square.parse('b4');
    expect(position.isSupplied(faris), isFalse);
    final after = position.play(Move.parse(step.expected.single));
    expect(after.isSupplied(faris), isTrue);
  });

  test('the cut-the-line lesson really cuts the enemy Faris', () {
    final step = lessons.firstWhere((l) => l.id == 'c2-cut').steps.single;
    final position = GameState.fromNotation(step.position);
    final faris = Square.parse('c4');
    expect(position.isSupplied(faris), isTrue);
    expect(
      position.play(Move.parse(step.expected.single)).isSupplied(faris),
      isFalse,
    );
  });
}
