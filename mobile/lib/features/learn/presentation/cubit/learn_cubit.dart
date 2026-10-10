import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import 'package:qala/features/learn/domain/entities/lesson.dart';
import 'package:qala/features/learn/domain/usecases/lesson_usecases.dart';

enum LearnStatus { loading, ready, failure }

class LearnState extends Equatable {
  const new({
    this.status = LearnStatus.loading,
    this.chapters = const [],
    this.stars = const {},
  });

  final LearnStatus status;
  final List<Chapter> chapters;
  final Map<String, int> stars;

  int starsInChapter(Chapter c) =>
      c.lessons.fold(0, (sum, l) => sum + (stars[l.id] ?? 0));

  @override
  List<Object?> get props => [status, chapters, stars];
}

class LearnCubit extends Cubit<LearnState> {
  new(this._getChapters, this._getStars) : super(const LearnState());

  final GetChapters _getChapters;
  final GetLessonStars _getStars;

  Future<void> load() async {
    final result = await _getChapters();
    emit(
      result.match(
        (_) => const LearnState(status: LearnStatus.failure),
        (chapters) => LearnState(
          status: LearnStatus.ready,
          chapters: chapters,
          stars: {
            for (final c in chapters)
              for (final l in c.lessons) l.id: _getStars(l.id),
          },
        ),
      ),
    );
  }
}
