import 'package:fpdart/fpdart.dart';

import 'package:qala/core/error/failure.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';
import 'package:qala/features/learn/domain/repositories/lesson_repository.dart';

class GetChapters {
  const new(this._repository);

  final LessonRepository _repository;

  Future<Either<Failure, List<Chapter>>> call() => _repository.chapters();
}

class GetLessonStars {
  const new(this._repository);

  final LessonRepository _repository;

  int call(String lessonId) => _repository.stars(lessonId);
}

/// Stars: 3 with no mistakes, 2 with up to two, otherwise 1. Seeing the
/// answer gives 1. Keeps the best result.
class CompleteLesson {
  const new(this._repository);

  final LessonRepository _repository;

  static int starsFor({required int mistakes, required bool answerShown}) {
    if (answerShown) return 1;
    if (mistakes == 0) return 3;
    return mistakes <= 2 ? 2 : 1;
  }

  Future<int> call(
    String lessonId, {
    required int mistakes,
    required bool answerShown,
  }) async {
    final stars = starsFor(mistakes: mistakes, answerShown: answerShown);
    if (stars > _repository.stars(lessonId)) {
      await _repository.saveStars(lessonId, stars);
    }
    return stars;
  }
}
