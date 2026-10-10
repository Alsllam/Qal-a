@Tags(['golden'])
library;

import 'dart:io';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:qala/app/app.dart';
import 'package:qala/app/di/injection.dart';
import 'package:qala/app/env/env.dart';
import 'package:qala/app/router/app_router.dart';
import 'package:qala/app/router/routes.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/settings/domain/entities/app_settings.dart';
import 'package:qala/features/settings/presentation/cubit/settings_cubit.dart';
import 'package:sembast/sembast_memory.dart';

Future<void> _loadFonts() async {
  for (final family in ['IBM Plex Sans Arabic', 'IBM Plex Sans']) {
    final file = family.replaceAll(' ', '');
    final loader = FontLoader(family);
    for (final w in [400, 500, 600, 700]) {
      final bytes = File('assets/fonts/$file-$w.ttf').readAsBytesSync();
      loader.addFont(Future.value(ByteData.sublistView(bytes)));
    }
    await loader.load();
  }
  // Material icons for buttons.
  final icons = FontLoader('MaterialIcons')
    ..addFont(rootBundle.load('fonts/MaterialIcons-Regular.otf'));
  await icons.load();
}

Future<void> _pumpScreen(
  WidgetTester tester, {
  required String location,
  required String language,
  required AppThemeMode theme,
}) async {
  tester.view.physicalSize = const Size(1080, 2340);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
  await getIt.reset();
  await configureDependencies(
    env: Env.fromDefines(Flavor.dev),
    store: MemoryKeyValueStore(),
    database: await databaseFactoryMemory.openDatabase('golden-$location.db'),
  );
  final settings = getIt<SettingsCubit>();
  await settings.setLanguage(language);
  await settings.setThemeMode(theme);
  await tester.pumpWidget(App(router: createRouter(initialLocation: location)));
  for (var i = 0; i < 10; i++) {
    await tester.pump(const Duration(milliseconds: 100));
  }
}

void main() {
  setUpAll(_loadFonts);

  testWidgets('home, Arabic, light', (tester) async {
    await _pumpScreen(
      tester,
      location: Routes.home,
      language: 'ar',
      theme: AppThemeMode.light,
    );
    await expectLater(
      find.byType(App),
      matchesGoldenFile('goldens/home_ar_light.png'),
    );
  });

  testWidgets('two-player game, English, dark', (tester) async {
    await _pumpScreen(
      tester,
      location: Routes.passAndPlay,
      language: 'en',
      theme: AppThemeMode.dark,
    );
    await expectLater(
      find.byType(App),
      matchesGoldenFile('goldens/game_en_dark.png'),
    );
  });

  testWidgets('lesson, Arabic, light', (tester) async {
    await _pumpScreen(
      tester,
      location: Routes.lesson('c2-supply'),
      language: 'ar',
      theme: AppThemeMode.light,
    );
    await tester.runAsync(
      () => Future<void>.delayed(const Duration(milliseconds: 200)),
    );
    for (var i = 0; i < 5; i++) {
      await tester.pump(const Duration(milliseconds: 100));
    }
    await expectLater(
      find.byType(App),
      matchesGoldenFile('goldens/lesson_ar_light.png'),
    );
  });
}
