import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

import 'helpers.dart';

void main() {
  group('Amir captured', () {
    test('capturing the Amir wins immediately', () {
      final state = position({'d4': 'A', 'd5': 'a'});
      final after = state.play(Move.parse('d4xd5'));
      expect(after.outcome, const Outcome(Side.south, EndReason.amirCaptured));
      expect(after.isOver, isTrue);
      expect(after.legalMoves, isEmpty);
      expect(after.amirSquare(Side.north), isNull);
    });

    test('shooting the Amir wins too', () {
      final state = position({'b3': 'A', 'c4': 'R', 'e6': 'a'});
      final after = state.play(Move.parse('c4*e6'));
      expect(after.outcome, const Outcome(Side.south, EndReason.amirCaptured));
    });

    test('no move can be played once the game is over', () {
      final over = position({'d4': 'A', 'd5': 'a'}).play(Move.parse('d4xd5'));
      expect(() => over.play(Move.parse('d4-d5')), throwsStateError);
    });
  });

  group('Qal\'a taken', () {
    test('the Amir walking into the enemy Qal\'a wins', () {
      final state = position({'d6': 'A', 'a7': 'a'});
      final after = state.play(Move.parse('d6-d7'));
      expect(after.outcome, const Outcome(Side.south, EndReason.qalaTaken));
    });

    test('North wins by taking South\'s Qal\'a', () {
      final state = position({'g1': 'A', 'd2': 'a'}, toMove: Side.north);
      final after = state.play(Move.parse('d2-d1'));
      expect(after.outcome, const Outcome(Side.north, EndReason.qalaTaken));
    });

    test('an unsupplied piece on the enemy Qal\'a does not win', () {
      final state = position({'a1': 'A', 'd4': 'F', 'g7': 'a'});
      final after = state.play(Move.parse('d4-d7'));
      expect(after.isOver, isFalse);
      expect(after.isSupplied(Square.parse('d7')), isFalse);
    });

    test('it wins as soon as supply reaches it at the end of a turn', () {
      final state = position({'e5': 'A', 'd7': 'F', 'a7': 'a'});
      expect(state.isSupplied(Square.parse('d7')), isFalse);
      final after = state.play(Move.parse('e5-e6'));
      expect(after.outcome, const Outcome(Side.south, EndReason.qalaTaken));
    });
  });

  group('no legal moves', () {
    test('a side with no legal move on its turn loses', () {
      const rules = RuleSet(version: 'test', amirIsSource: false);
      // North Amir in the a7 corner, boxed in by South pieces it cannot
      // capture (it is unsupplied when it is not a source).
      final state = position(
        {'g1': 'A', 'a6': 'J', 'b7': 'J', 'b5': 'J', 'a7': 'a'},
        rules: rules,
      );
      final after = state.play(Move.parse('b5-b6'));
      expect(after.outcome, const Outcome(Side.south, EndReason.noLegalMoves));
    });
  });

  group('ply limit', () {
    const rules = RuleSet(version: 'test', plyLimit: 10);

    test('the game is not over one ply before the limit', () {
      final state = position({'a1': 'A', 'g7': 'a'}, ply: 8, rules: rules);
      expect(state.play(Move.parse('a1-a2')).isOver, isFalse);
    });

    test('more Wells wins at the limit', () {
      final state = position(
        {'a1': 'A', 'c4': 'J', 'g7': 'a', 'f7': 'j', 'e7': 'j'},
        ply: 9,
        rules: rules,
      );
      final after = state.play(Move.parse('a1-a2'));
      expect(after.outcome, const Outcome(Side.south, EndReason.plyLimitWells));
    });

    test('then more pieces wins', () {
      final state = position(
        {'a1': 'A', 'g7': 'a', 'f7': 'j'},
        ply: 9,
        rules: rules,
      );
      final after = state.play(Move.parse('a1-a2'));
      expect(
          after.outcome, const Outcome(Side.north, EndReason.plyLimitMaterial));
    });

    test('otherwise it is a draw', () {
      final state = position({'a1': 'A', 'g7': 'a'}, ply: 9, rules: rules);
      final after = state.play(Move.parse('a1-a2'));
      expect(after.outcome, const Outcome(null, EndReason.plyLimitDraw));
      expect(after.outcome!.isDraw, isTrue);
    });

    test('a win on the last ply beats the limit', () {
      final state = position({'d6': 'A', 'a7': 'a'}, ply: 9, rules: rules);
      final after = state.play(Move.parse('d6-d7'));
      expect(after.outcome!.reason, EndReason.qalaTaken);
    });
  });
}
