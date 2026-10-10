import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:game_core/game_core.dart';

import 'package:qala/core/widgets/board/board_view_model.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';
import 'package:qala/features/learn/domain/usecases/lesson_usecases.dart';

sealed class LessonEvent extends Equatable {
  const new();

  @override
  List<Object?> get props => const [];
}

final class LessonSquareTapped extends LessonEvent {
  const new(this.square);

  final Square square;

  @override
  List<Object?> get props => [square];
}

final class LessonNextPressed extends LessonEvent {
  const new();
}

final class LessonShowAnswerPressed extends LessonEvent {
  const new();
}

enum StepStatus { playing, solved, wrong, completed }

class LessonState extends Equatable {
  const new({
    required this.lesson,
    required this.stepIndex,
    required this.position,
    this.status = StepStatus.playing,
    this.selected,
    this.lastMove,
    this.mistakes = 0,
    this.answerShown = false,
    this.hintSquares = const {},
    this.stars = 0,
  });

  final Lesson lesson;
  final int stepIndex;
  final GameState position;
  final StepStatus status;
  final Square? selected;
  final Move? lastMove;
  final int mistakes;
  final bool answerShown;
  final Set<Square> hintSquares;

  /// Stars earned, set when [status] is completed.
  final int stars;

  LessonStep get step => lesson.steps[stepIndex];
  bool get isLastStep => stepIndex == lesson.steps.length - 1;
  bool get canAdvance => step.isExplanation || status == StepStatus.solved;

  List<Move> get targets =>
      selected == null ? const [] : position.legalMovesFrom(selected!);

  LessonState copyWith({
    int? stepIndex,
    GameState? position,
    StepStatus? status,
    Square? Function()? selected,
    Move? Function()? lastMove,
    int? mistakes,
    bool? answerShown,
    Set<Square>? hintSquares,
    int? stars,
  }) => LessonState(
    lesson: lesson,
    stepIndex: stepIndex ?? this.stepIndex,
    position: position ?? this.position,
    status: status ?? this.status,
    selected: selected == null ? this.selected : selected(),
    lastMove: lastMove == null ? this.lastMove : lastMove(),
    mistakes: mistakes ?? this.mistakes,
    answerShown: answerShown ?? this.answerShown,
    hintSquares: hintSquares ?? this.hintSquares,
    stars: stars ?? this.stars,
  );

  @override
  List<Object?> get props => [
    lesson,
    stepIndex,
    position,
    status,
    selected,
    lastMove,
    mistakes,
    answerShown,
    hintSquares,
    stars,
  ];
}

/// Runs one lesson: show → try → check, step by step.
class LessonBloc extends Bloc<LessonEvent, LessonState> {
  new({required Lesson lesson, required CompleteLesson completeLesson})
    : _complete = completeLesson,
      super(
        LessonState(
          lesson: lesson,
          stepIndex: 0,
          position: GameState.fromNotation(lesson.steps.first.position),
        ),
      ) {
    on<LessonSquareTapped>(_onTap);
    on<LessonNextPressed>(_onNext);
    on<LessonShowAnswerPressed>(_onShowAnswer);
  }

  final CompleteLesson _complete;

  void _onTap(LessonSquareTapped event, Emitter<LessonState> emit) {
    if (state.step.isExplanation || state.status == StepStatus.solved) return;
    final result = BoardSelection.tap(
      state.position,
      state.selected,
      event.square,
    );
    final move = result.move;
    if (move == null) {
      emit(
        state.copyWith(
          selected: () => result.selected,
          status: StepStatus.playing,
        ),
      );
      return;
    }
    if (state.step.expected.contains(move.notation)) {
      emit(
        state.copyWith(
          position: state.position.play(move),
          lastMove: () => move,
          selected: () => null,
          status: StepStatus.solved,
          hintSquares: const {},
        ),
      );
    } else {
      emit(
        state.copyWith(
          selected: () => null,
          status: StepStatus.wrong,
          mistakes: state.mistakes + 1,
        ),
      );
    }
  }

  Future<void> _onNext(
    LessonNextPressed event,
    Emitter<LessonState> emit,
  ) async {
    if (!state.canAdvance) return;
    if (state.isLastStep) {
      final stars = await _complete(
        state.lesson.id,
        mistakes: state.mistakes,
        answerShown: state.answerShown,
      );
      emit(state.copyWith(status: StepStatus.completed, stars: stars));
      return;
    }
    final next = state.stepIndex + 1;
    emit(
      state.copyWith(
        stepIndex: next,
        position: GameState.fromNotation(state.lesson.steps[next].position),
        status: StepStatus.playing,
        selected: () => null,
        lastMove: () => null,
        hintSquares: const {},
      ),
    );
  }

  void _onShowAnswer(LessonShowAnswerPressed event, Emitter<LessonState> emit) {
    if (state.step.isExplanation) return;
    final answer = Move.parse(state.step.expected.first);
    emit(
      state.copyWith(answerShown: true, hintSquares: {answer.from, answer.to}),
    );
  }
}
