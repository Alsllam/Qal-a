import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import 'package:qala/app/di/injection.dart';
import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/core/l10n/l10n.dart';
import 'package:qala/core/widgets/board/board_view_model.dart';
import 'package:qala/core/widgets/board/qala_board.dart';
import 'package:qala/features/game/presentation/widgets/piece_glyph.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';
import 'package:qala/features/learn/presentation/bloc/lesson_bloc.dart';

class LessonPage extends StatelessWidget {
  const new({required this.lesson, super.key});

  final Lesson lesson;

  @override
  Widget build(BuildContext context) => BlocProvider(
    create: (_) => LessonBloc(lesson: lesson, completeLesson: getIt()),
    child: const LessonView(),
  );
}

class LessonView extends StatelessWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final lang = Localizations.localeOf(context).languageCode;
    final tokens = context.tokens;
    return BlocBuilder<LessonBloc, LessonState>(
      builder: (context, state) {
        final bloc = context.read<LessonBloc>();
        final completed = state.status == StepStatus.completed;
        final String message;
        if (completed) {
          message = l10n.lessonComplete;
        } else if (state.status == StepStatus.solved) {
          message = state.step.success?.of(lang) ?? state.step.text.of(lang);
        } else if (state.status == StepStatus.wrong) {
          message = l10n.lessonTryAgain;
        } else {
          message = state.step.text.of(lang);
        }
        return Scaffold(
          appBar: AppBar(title: Text(state.lesson.title.of(lang))),
          body: SafeArea(
            child: Column(
              children: [
                Padding(
                  padding: const EdgeInsets.all(AppSpacing.s16),
                  child: Card(
                    color: state.status == StepStatus.wrong
                        ? tokens.danger.withValues(alpha: .1)
                        : tokens.surfaceRaised,
                    child: Padding(
                      padding: const EdgeInsets.all(AppSpacing.s16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            l10n.lessonStep(
                              state.stepIndex + 1,
                              state.lesson.steps.length,
                            ),
                            style: Theme.of(context).textTheme.labelMedium,
                          ),
                          const SizedBox(height: AppSpacing.s8),
                          Text(
                            message,
                            key: const Key('lesson-message'),
                            style: Theme.of(context).textTheme.bodyLarge,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                Expanded(
                  child: Center(
                    child: Padding(
                      padding: const EdgeInsets.all(AppSpacing.s8),
                      child: QalaBoard(
                        model: BoardViewModel(
                          position: state.position,
                          selected: state.selected,
                          targets: state.targets,
                          lastMove: state.lastMove,
                          hintSquares: state.hintSquares,
                        ),
                        glyph: (t) => pieceGlyph(context, t),
                        onSquareTap: (s) => bloc.add(LessonSquareTapped(s)),
                      ),
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.all(AppSpacing.s16),
                  child: Row(
                    children: [
                      if (!state.step.isExplanation && !state.canAdvance)
                        TextButton(
                          onPressed: () =>
                              bloc.add(const LessonShowAnswerPressed()),
                          child: Text(l10n.lessonShowAnswer),
                        ),
                      const Spacer(),
                      FilledButton(
                        key: const Key('lesson-next'),
                        onPressed: completed
                            ? () => context.pop()
                            : state.canAdvance
                            ? () => bloc.add(const LessonNextPressed())
                            : null,
                        child: Text(
                          completed ? l10n.backHome : l10n.lessonNext,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
