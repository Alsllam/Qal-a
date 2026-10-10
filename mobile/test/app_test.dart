import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:qala/app/app.dart';
import 'package:qala/app/di/injection.dart';
import 'package:qala/app/env/env.dart';
import 'package:qala/app/router/app_router.dart';
import 'package:qala/app/router/routes.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:sembast/sembast_memory.dart';

Future<void> setUpApp() async {
  await getIt.reset();
  await configureDependencies(
    env: Env.fromDefines(Flavor.dev),
    store: MemoryKeyValueStore(),
    database: await databaseFactoryMemory.openDatabase('test.db'),
  );
}

void main() {
  setUp(setUpApp);

  testWidgets('home shows the play modes', (tester) async {
    await tester.pumpWidget(
      App(router: createRouter(initialLocation: Routes.home)),
    );
    await tester.pump();
    expect(find.byKey(const Key('home-learn')), findsOneWidget);
    expect(find.byKey(const Key('home-ladder')), findsOneWidget);
    expect(find.byKey(const Key('home-pvp')), findsOneWidget);
  });

  testWidgets('two players opens a game with South to move', (tester) async {
    await tester.pumpWidget(
      App(router: createRouter(initialLocation: Routes.home)),
    );
    await tester.pump();
    await tester.tap(find.byKey(const Key('home-pvp')));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.byKey(const Key('game-status')), findsOneWidget);
    expect(find.textContaining('0/60'), findsOneWidget);
  });

  testWidgets('the ladder lists the opponents', (tester) async {
    await tester.pumpWidget(
      App(router: createRouter(initialLocation: Routes.ladder)),
    );
    await tester.pump();
    expect(find.text('Shepherd'), findsOneWidget);
  });
}
