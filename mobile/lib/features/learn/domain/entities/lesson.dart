import 'package:equatable/equatable.dart';

import 'package:qala/core/domain/localized_text.dart';

/// One step of a lesson: a text, a position, and the moves that solve it.
/// A step with no expected moves is an explanation (the player taps Next).
class LessonStep extends Equatable {
  const new({
    required this.text,
    required this.position,
    this.expected = const [],
    this.success,
  });

  final LocalizedText text;

  /// Position notation (game_core format).
  final String position;

  /// Accepted moves in notation, e.g. `d3-d4`.
  final List<String> expected;
  final LocalizedText? success;

  bool get isExplanation => expected.isEmpty;

  @override
  List<Object?> get props => [text, position, expected, success];
}

class Lesson extends Equatable {
  const new({required this.id, required this.title, required this.steps});

  final String id;
  final LocalizedText title;
  final List<LessonStep> steps;

  @override
  List<Object?> get props => [id, title, steps];
}

class Chapter extends Equatable {
  const new({required this.number, required this.title, required this.lessons});

  final int number;
  final LocalizedText title;
  final List<Lesson> lessons;

  @override
  List<Object?> get props => [number, title, lessons];
}
