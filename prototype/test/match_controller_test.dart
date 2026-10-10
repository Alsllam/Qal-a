import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:game_core/game_core.dart';
import 'package:qala_prototype/src/match_controller.dart';
import 'package:qala_prototype/src/strings.dart';

Square sq(String name) => Square.parse(name);

/// Fake AI: plays the first legal move instantly.
Future<Move> firstMove(GameState state, AiLevel level, int seed) async =>
    state.legalMoves.first;

/// Fake clock advanced by hand.
class FakeClock {
  DateTime now = DateTime(2026, 10, 10, 12);
  DateTime call() => now;
  void advance(int seconds) => now = now.add(Duration(seconds: seconds));
}

MatchController pvp([FakeClock? clock]) =>
    MatchController(const MatchConfig(), clock: clock?.call);

MatchController vsAi(Side human) => MatchController(
  MatchConfig(aiLevel: AiLevel.easy, humanSide: human),
  findAiMove: firstMove,
  aiMinDelay: Duration.zero,
);

/// Lets the fake AI future and its delayed play complete.
Future<void> settle() => Future<void>.delayed(const Duration(milliseconds: 5));

void main() {
  group('pass and play', () {
    test('tapping an own piece selects it and shows its moves', () {
      final c = pvp();
      c.tap(sq('b1'));
      expect(c.selected, sq('b1'));
      expect(c.selectedMoves.map((m) => m.notation).toSet(), {
        'b1-a1',
        'b1-b2',
        'b1-b3',
        'b1-b4',
      });
    });

    test('tapping a target plays the move and passes the turn', () {
      final c = pvp();
      c.tap(sq('b1'));
      c.tap(sq('b4'));
      expect(c.state.at(sq('b4'))?.type, PieceType.faris);
      expect(c.state.toMove, Side.north);
      expect(c.selected, isNull);
      expect(c.lastMove?.notation, 'b1-b4');
    });

    test('enemy pieces and empty squares cannot be selected', () {
      final c = pvp();
      c.tap(sq('d7'));
      expect(c.selected, isNull);
      c.tap(sq('d4'));
      expect(c.selected, isNull);
    });

    test('tapping the selected piece again deselects it', () {
      final c = pvp();
      c.tap(sq('c2'));
      c.tap(sq('c2'));
      expect(c.selected, isNull);
    });

    test(
      'an unsupplied piece tapping an enemy gets the no-water hint',
      () async {
        final c = pvp();
        final hints = <Hint>[];
        c.hints.listen(hints.add);
        // South Faris to b4 (cut off), North Faris to b5 next to it.
        c.tap(sq('b1'));
        c.tap(sq('b4'));
        c.tap(sq('b7'));
        c.tap(sq('b5'));
        c.tap(sq('b4'));
        c.tap(sq('b5'));
        await settle();
        expect(hints, [Hint.noWater]);
        expect(c.state.at(sq('b5'))?.side, Side.north, reason: 'not captured');
        expect(c.record().illegalCaptureAttempts, 1);
      },
    );

    test('undo takes back one ply', () {
      final c = pvp();
      expect(c.canUndo, isFalse);
      c.tap(sq('c2'));
      c.tap(sq('c3'));
      expect(c.canUndo, isTrue);
      c.undo();
      expect(c.state.toNotation(), GameState.initial().toNotation());
      expect(c.log, isEmpty);
    });

    test('restart returns to the opening position', () {
      final c = pvp();
      c.tap(sq('c2'));
      c.tap(sq('c3'));
      c.restart();
      expect(c.state.ply, 0);
      expect(c.lastMove, isNull);
    });

    test('move times are recorded from the clock', () {
      final clock = FakeClock();
      final c = pvp(clock);
      clock.advance(12);
      c.tap(sq('c2'));
      c.tap(sq('c3'));
      clock.advance(30);
      c.tap(sq('c6'));
      c.tap(sq('c5'));
      expect(c.log.map((e) => e.elapsed.inSeconds), [12, 30]);
    });
  });

  group('against the AI', () {
    test('the AI replies after the human move', () async {
      final c = vsAi(Side.south);
      c.tap(sq('c2'));
      c.tap(sq('c3'));
      expect(c.aiThinking, isTrue);
      c.tap(sq('e2')); // ignored while the AI thinks
      expect(c.selected, isNull);
      await settle();
      expect(c.aiThinking, isFalse);
      expect(c.state.toMove, Side.south);
      expect(c.log.last.side, Side.north);
    });

    test('the AI moves first when the human plays North', () async {
      final c = vsAi(Side.north);
      expect(c.isHumanTurn, isFalse);
      await settle();
      expect(c.state.ply, 1);
      expect(c.isHumanTurn, isTrue);
    });

    test('undo goes back to the human\'s previous turn', () async {
      final c = vsAi(Side.south);
      c.tap(sq('c2'));
      c.tap(sq('c3'));
      await settle();
      expect(c.state.ply, 2);
      c.undo();
      expect(c.state.ply, 0);
      expect(c.isHumanTurn, isTrue);
    });

    test('a result from before a restart is ignored', () async {
      // The first search answers late; the one after the restart never does.
      final gates = [Completer<Move>(), Completer<Move>()];
      var calls = 0;
      final c = MatchController(
        const MatchConfig(aiLevel: AiLevel.easy, humanSide: Side.north),
        findAiMove: (state, level, seed) => gates[calls++].future,
        aiMinDelay: Duration.zero,
      );
      c.restart();
      gates[0].complete(GameState.initial().legalMoves.first);
      await settle();
      expect(c.state.ply, 0);
    });
  });

  test('the record summarises the game for the playtest log', () {
    final clock = FakeClock();
    final c = pvp(clock);
    clock.advance(5);
    c.tap(sq('d2'));
    c.tap(sq('c3'));
    final record = c.record();
    final text = record.toString();
    expect(text, contains('rules: 0.6 | mode: pvp'));
    expect(text, contains('first move: d2-c3'));
    expect(text, contains('rami: 1 moves, 0 shots'));
    expect(text, contains('1. Rd2-c3 (5s)'));
    expect(record.csv, '0.6,pvp,-,-,1,0.1,5,d2-c3,1,0,0');
    expect(text, contains('final position: '));
  });
}
