import 'package:fpdart/fpdart.dart';

import 'package:qala/core/error/failure.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';

abstract interface class LessonRepository {
  Future<Either<Failure, List<Chapter>>> chapters();

  /// Best stars earned (0–3).
  int stars(String lessonId);
  Future<void> saveStars(String lessonId, int stars);
}
