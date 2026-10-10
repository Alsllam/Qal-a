import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:game_core/game_core.dart';
import 'package:qala_prototype/main.dart';
import 'package:qala_prototype/src/match_controller.dart';
import 'package:qala_prototype/src/screens/game_screen.dart';
import 'package:qala_prototype/src/strings.dart';

void main() {
  testWidgets('home screen starts in Arabic and switches to English', (
    tester,
  ) async {
    await tester.pumpWidget(const QalaPrototypeApp());
    expect(find.text('قلعة'), findsOneWidget);
    await tester.tap(find.byKey(const Key('language')));
    await tester.pumpAndSettle();
    expect(find.text("Qal'a"), findsOneWidget);
    expect(find.text('2 players (pass & play)'), findsOneWidget);
  });

  testWidgets('choosing the AI shows level and side options', (tester) async {
    await tester.pumpWidget(const QalaPrototypeApp());
    await tester.tap(find.byKey(const Key('language')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('mode-ai')));
    await tester.pumpAndSettle();
    expect(find.text('Medium'), findsOneWidget);
    expect(find.text('North (indigo)'), findsOneWidget);
  });

  testWidgets('start opens a game with South to move', (tester) async {
    await tester.pumpWidget(const QalaPrototypeApp());
    await tester.tap(find.byKey(const Key('language')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('start')));
    await tester.tap(find.byKey(const Key('start')));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.byKey(const Key('board')), findsOneWidget);
    expect(find.textContaining('South (sand) to move'), findsOneWidget);
    expect(find.textContaining('0/60'), findsOneWidget);
  });

  testWidgets('the status line and undo follow the controller', (tester) async {
    final controller = MatchController(const MatchConfig());
    await tester.pumpWidget(
      MaterialApp(
        home: GameScreen(
          config: const MatchConfig(),
          strings: const S(Lang.en),
          controller: controller,
        ),
      ),
    );
    await tester.pump();
    final undo = find.byKey(const Key('undo'));
    expect(tester.widget<OutlinedButton>(undo).onPressed, isNull);

    controller.tap(Square.parse('c2'));
    controller.tap(Square.parse('c3'));
    await tester.pump();
    expect(find.textContaining('North (indigo) to move'), findsOneWidget);
    expect(find.textContaining('1/60'), findsOneWidget);
    expect(tester.widget<OutlinedButton>(undo).onPressed, isNotNull);
    controller.dispose();
  });

  testWidgets('the end dialog names the winner and offers a rematch', (
    tester,
  ) async {
    // South Amir on d4 can capture the North Amir on d5.
    const config = MatchConfig(startPosition: '7/7/3a3/3A3/7/7/7 s 0 0:0');
    final controller = MatchController(config);
    await tester.pumpWidget(
      MaterialApp(
        home: GameScreen(
          config: config,
          strings: const S(Lang.en),
          controller: controller,
        ),
      ),
    );
    await tester.pump();
    controller.tap(Square.parse('d4'));
    controller.tap(Square.parse('d5'));
    await tester.pump(const Duration(seconds: 1));
    await tester.pump();
    expect(find.text('South (sand) wins!'), findsOneWidget);
    expect(find.text('The Amir was captured.'), findsOneWidget);
    await tester.tap(find.text('Play again'));
    // The Flame game loop never settles, so pump fixed frames.
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.text('South (sand) wins!'), findsNothing);
    expect(controller.state.ply, 0);
    controller.dispose();
  });
}
