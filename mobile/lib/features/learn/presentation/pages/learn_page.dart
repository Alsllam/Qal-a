import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import 'package:qala/app/router/routes.dart';
import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/core/l10n/l10n.dart';
import 'package:qala/features/learn/presentation/cubit/learn_cubit.dart';

class LearnPage extends StatelessWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final lang = Localizations.localeOf(context).languageCode;
    return Scaffold(
      appBar: AppBar(title: Text(l10n.homeLearnTitle)),
      body: BlocBuilder<LearnCubit, LearnState>(
        builder: (context, state) => switch (state.status) {
          LearnStatus.loading => const Center(
            child: CircularProgressIndicator(),
          ),
          LearnStatus.failure => Center(
            child: FilledButton(
              onPressed: () => context.read<LearnCubit>().load(),
              child: Text(l10n.retry),
            ),
          ),
          LearnStatus.ready => ListView(
            padding: const EdgeInsets.all(AppSpacing.s16),
            children: [
              for (final chapter in state.chapters) ...[
                Padding(
                  padding: const EdgeInsetsDirectional.only(
                    top: AppSpacing.s16,
                    bottom: AppSpacing.s8,
                  ),
                  child: Text(
                    [
                      l10n.learnChapter(chapter.number),
                      chapter.title.of(lang),
                    ].join(' · '),
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                for (final lesson in chapter.lessons)
                  Card(
                    child: ListTile(
                      title: Text(lesson.title.of(lang)),
                      subtitle: Text(
                        l10n.learnStars(state.stars[lesson.id] ?? 0),
                      ),
                      trailing: _Stars(state.stars[lesson.id] ?? 0),
                      onTap: () async {
                        await context.push(Routes.lesson(lesson.id));
                        if (context.mounted) {
                          await context.read<LearnCubit>().load();
                        }
                      },
                    ),
                  ),
              ],
            ],
          ),
        },
      ),
    );
  }
}

class _Stars extends StatelessWidget {
  const new(this.count);

  final int count;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      for (var i = 0; i < 3; i++)
        Icon(
          i < count ? Icons.star_rounded : Icons.star_outline_rounded,
          color: context.tokens.selection,
          size: 20,
        ),
    ],
  );
}
