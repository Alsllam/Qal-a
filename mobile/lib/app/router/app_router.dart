import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:qala/app/di/injection.dart';
import 'package:qala/app/router/routes.dart';
import 'package:qala/core/l10n/l10n.dart';
import 'package:qala/features/game/domain/entities/game_mode.dart';
import 'package:qala/features/game/domain/entities/opponent.dart';
import 'package:qala/features/game/presentation/pages/game_page.dart';
import 'package:qala/features/home/presentation/pages/home_page.dart';
import 'package:qala/features/ladder/presentation/cubit/ladder_cubit.dart';
import 'package:qala/features/ladder/presentation/pages/ladder_page.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';
import 'package:qala/features/learn/domain/usecases/lesson_usecases.dart';
import 'package:qala/features/learn/presentation/cubit/learn_cubit.dart';
import 'package:qala/features/learn/presentation/pages/learn_page.dart';
import 'package:qala/features/learn/presentation/pages/lesson_page.dart';
import 'package:qala/features/settings/presentation/pages/settings_page.dart';
import 'package:qala/features/splash/presentation/pages/splash_page.dart';

GoRouter createRouter({String initialLocation = Routes.splash}) => GoRouter(
  initialLocation: initialLocation,
  routes: [
    GoRoute(path: Routes.splash, builder: (_, _) => const SplashPage()),
    GoRoute(
      path: Routes.home,
      builder: (_, _) => const HomePage(),
      routes: [
        GoRoute(
          path: 'learn',
          builder: (_, _) => BlocProvider(
            create: (_) {
              final cubit = getIt<LearnCubit>();
              unawaited(cubit.load());
              return cubit;
            },
            child: const LearnPage(),
          ),
          routes: [
            GoRoute(
              path: 'lesson/:id',
              builder: (_, state) =>
                  _LessonLoader(id: state.pathParameters['id']!),
            ),
          ],
        ),
        GoRoute(
          path: 'ladder',
          builder: (_, _) => BlocProvider(
            create: (_) => getIt<LadderCubit>(),
            child: const LadderPage(),
          ),
          routes: [
            GoRoute(
              path: ':level',
              builder: (_, state) {
                final level = int.parse(state.pathParameters['level']!);
                return GamePage(
                  key: ValueKey('ladder-$level'),
                  mode: LadderGame(Opponents.byLevel(level)),
                );
              },
            ),
          ],
        ),
        GoRoute(
          path: 'play',
          builder: (_, _) => const GamePage(mode: PassAndPlay()),
        ),
        GoRoute(path: 'settings', builder: (_, _) => const SettingsPage()),
      ],
    ),
  ],
);

/// Loads a lesson by id (works for deep links too).
class _LessonLoader extends StatefulWidget {
  const new({required this.id});

  final String id;

  @override
  State<_LessonLoader> createState() => _LessonLoaderState();
}

class _LessonLoaderState extends State<_LessonLoader> {
  late final Future<Lesson?> _lesson = _load();

  Future<Lesson?> _load() async {
    final result = await getIt<GetChapters>()();
    return result.match<Lesson?>(
      (_) => null,
      (chapters) => chapters
          .expand((c) => c.lessons)
          .where((l) => l.id == widget.id)
          .firstOrNull,
    );
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<Lesson?>(
    future: _lesson,
    builder: (context, snapshot) {
      final lesson = snapshot.data;
      if (lesson != null) return LessonPage(lesson: lesson);
      return Scaffold(
        appBar: AppBar(),
        body: Center(
          child: snapshot.connectionState == ConnectionState.done
              ? Text(context.l10n.errorGeneric)
              : const CircularProgressIndicator(),
        ),
      );
    },
  );
}
