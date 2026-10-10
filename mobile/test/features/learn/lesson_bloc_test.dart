import 'package:bloc_test/bloc_test.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:game_core/game_core.dart';
import 'package:qala/core/domain/localized_text.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/learn/data/datasources/lesson_asset_data_source.dart';
import 'package:qala/features/learn/data/repositories/lesson_repository_impl.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';
import 'package:qala/features/learn/domain/usecases/lesson_usecases.dart';
import 'package:qala/features/learn/presentation/bloc/lesson_bloc.dart';

const _t = LocalizedText(en: 'x', ar: 'س');
const lesson = Lesson(
  id: 'test',
  title: _t,
  steps: [
    LessonStep(text: _t, position: '6a/7/7/7/3A3/7/7 s 0 0:0'),
    LessonStep(
      text: _t,
      position: '3a3/7/7/3j3/3A3/7/7 s 0 0:0',
      expected: ['d3xd4'],
    ),
  ],
);

Square sq(String n) => Square.parse(n);

void main() {
  late LessonRepositoryImpl repo;

  setUp(() {
    repo = LessonRepositoryImpl(
      LessonAssetDataSource(rootBundle),
      MemoryKeyValueStore(),
    );
  });

  LessonBloc build() =>
      LessonBloc(lesson: lesson, completeLesson: CompleteLesson(repo));

  blocTest<LessonBloc, LessonState>(
    'explanation steps advance with Next',
    build: build,
    act: (b) => b.add(const LessonNextPressed()),
    verify: (b) => expect(b.state.stepIndex, 1),
  );

  blocTest<LessonBloc, LessonState>(
    'a wrong move counts a mistake and does not move',
    build: build,
    act: (b) => b
      ..add(const LessonNextPressed())
      ..add(LessonSquareTapped(sq('d3')))
      ..add(LessonSquareTapped(sq('c3'))),
    verify: (b) {
      expect(b.state.status, StepStatus.wrong);
      expect(b.state.mistakes, 1);
      expect(b.state.position.at(sq('d3'))?.type, PieceType.amir);
    },
  );

  blocTest<LessonBloc, LessonState>(
    'the right move solves the step; finishing awards three stars',
    build: build,
    act: (b) => b
      ..add(const LessonNextPressed())
      ..add(LessonSquareTapped(sq('d3')))
      ..add(LessonSquareTapped(sq('d4')))
      ..add(const LessonNextPressed()),
    verify: (b) {
      expect(b.state.status, StepStatus.completed);
      expect(b.state.stars, 3);
      expect(repo.stars('test'), 3);
    },
  );

  blocTest<LessonBloc, LessonState>(
    'showing the answer highlights it and limits the stars to one',
    build: build,
    act: (b) => b
      ..add(const LessonNextPressed())
      ..add(const LessonShowAnswerPressed())
      ..add(LessonSquareTapped(sq('d3')))
      ..add(LessonSquareTapped(sq('d4')))
      ..add(const LessonNextPressed()),
    verify: (b) {
      expect(b.state.stars, 1);
    },
  );

  test('stars rule', () {
    expect(CompleteLesson.starsFor(mistakes: 0, answerShown: false), 3);
    expect(CompleteLesson.starsFor(mistakes: 2, answerShown: false), 2);
    expect(CompleteLesson.starsFor(mistakes: 5, answerShown: false), 1);
    expect(CompleteLesson.starsFor(mistakes: 0, answerShown: true), 1);
  });
}
