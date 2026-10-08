import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

import 'helpers.dart';

void main() {
  group('opening position', () {
    test('South has exactly these 14 moves', () {
      expect(moves(GameState.initial()), {
        'b1-a1', 'b1-b2', 'b1-b3', 'b1-b4', //
        'f1-g1', 'f1-f2', 'f1-f3', 'f1-f4',
        'c2-b2', 'c2-c3', 'e2-f2', 'e2-e3',
        'd2-c3', 'd2-e3',
      });
    });

    test('North also has 14 moves after a quiet South move', () {
      final state = GameState.initial().play(Move.parse('f1-f2'));
      expect(state.toMove, Side.north);
      expect(state.legalMoves, hasLength(14));
      expect(moves(state, 'b7'), {'b7-a7', 'b7-b6', 'b7-b5', 'b7-b4'});
    });
  });

  group('Amir', () {
    test('steps one square in all eight directions', () {
      final state = position({'d4': 'A', 'a7': 'a'});
      expect(moves(state, 'd4'), {
        'd4-d5', 'd4-e5', 'd4-e4', 'd4-e3', //
        'd4-d3', 'd4-c3', 'd4-c4', 'd4-c5',
      });
    });

    test('has three moves in a corner', () {
      expect(moves(position({'a1': 'A', 'g7': 'a'}), 'a1'), hasLength(3));
    });

    test('captures adjacent enemies (it is always supplied)', () {
      final state = position({'d4': 'A', 'e5': 'j', 'a7': 'a'});
      expect(moves(state, 'd4'), contains('d4xe5'));
    });
  });

  group('Jundi', () {
    test('steps orthogonally only', () {
      final state = position({'d4': 'J', 'a1': 'A', 'g7': 'a'});
      expect(moves(state, 'd4'), {'d4-d5', 'd4-e4', 'd4-d3', 'd4-c4'});
    });

    test('captures orthogonally when supplied', () {
      final state =
          position({'c4': 'J', 'c5': 'j', 'd5': 'j', 'a1': 'A', 'g7': 'a'});
      // c4 is a Well, so the Jundi on it is supplied.
      expect(moves(state, 'c4'), {'c4xc5', 'c4-d4', 'c4-c3', 'c4-b4'});
    });

    test('cannot capture when unsupplied', () {
      final state = position({'g4': 'J', 'g5': 'j', 'a1': 'A', 'a7': 'a'});
      expect(moves(state, 'g4'), {'g4-f4', 'g4-g3'});
    });
  });

  group('Faris', () {
    test('slides up to three squares orthogonally', () {
      final state = position({'a4': 'F', 'g1': 'A', 'g7': 'a'});
      expect(moves(state, 'a4'), {
        'a4-a5', 'a4-a6', 'a4-a7', //
        'a4-a3', 'a4-a2', 'a4-a1',
        'a4-b4', 'a4-c4', 'a4-d4',
      });
    });

    test('is blocked by its own pieces', () {
      final state =
          position({'a4': 'F', 'a6': 'J', 'b4': 'J', 'g1': 'A', 'g7': 'a'});
      expect(moves(state, 'a4'), {'a4-a5', 'a4-a3', 'a4-a2', 'a4-a1'});
    });

    test('captures the first enemy in line when supplied, never beyond', () {
      // Faris on the c4 Well is supplied.
      final state =
          position({'c4': 'F', 'c6': 'j', 'c7': 'j', 'g1': 'A', 'g7': 'a'});
      final fromC4 = moves(state, 'c4');
      expect(fromC4, containsAll(['c4-c5', 'c4xc6']));
      expect(fromC4, isNot(contains('c4xc7')));
    });

    test('cannot capture when unsupplied', () {
      final state = position({'a4': 'F', 'a6': 'j', 'g1': 'A', 'g7': 'a'});
      expect(moves(state, 'a4'), contains('a4-a5'));
      expect(moves(state, 'a4'), isNot(contains('a4xa6')));
    });

    test('range follows the rule set', () {
      final rules = RuleSet.standard.copyWith(farisRange: 2);
      final state = position({'a4': 'F', 'g1': 'A', 'g7': 'a'}, rules: rules);
      expect(moves(state, 'a4'), hasLength(6));
    });
  });

  group('Rami', () {
    test('steps one square diagonally to empty squares only', () {
      final state =
          position({'d4': 'R', 'e5': 'J', 'c3': 'j', 'a1': 'A', 'g7': 'a'});
      expect(moves(state, 'd4'), {'d4-c5', 'd4-e3'});
    });

    test('shoots enemies exactly two squares away in all eight directions', () {
      final state = position({
        'd4': 'R', 'e4': 'J', // e4 is a Well: supplies the Rami
        'd6': 'j', 'f6': 'j', 'b6': 'j', 'b4': 'j', 'b2': 'j', 'd2': 'j',
        'f2': 'j',
        'a1': 'A', 'g7': 'a',
      });
      final shots = state.legalMoves
          .where((m) => m.kind == MoveKind.shot)
          .map((m) => m.notation)
          .toSet();
      expect(shots,
          {'d4*d6', 'd4*f6', 'd4*b6', 'd4*b4', 'd4*b2', 'd4*d2', 'd4*f2'});
    });

    test('cannot shoot through a piece, at distance one, or at friends', () {
      final state = position({
        'd4': 'R', 'e4': 'J',
        'd5': 'J', 'd6': 'j', // blocked by own piece
        'c5': 'j', // distance 1
        'b2': 'J', // friendly target
        'a1': 'A', 'g7': 'a',
      });
      expect(state.legalMoves.where((m) => m.kind == MoveKind.shot), isEmpty);
    });

    test('cannot shoot when unsupplied', () {
      final state = position({'g4': 'R', 'g6': 'j', 'a1': 'A', 'a7': 'a'});
      expect(moves(state, 'g4'), {'g4-f5', 'g4-f3'});
    });

    test('stays on its square after shooting', () {
      final state =
          position({'d4': 'R', 'e4': 'J', 'd6': 'j', 'a1': 'A', 'g7': 'a'});
      final after = state.play(Move.parse('d4*d6'));
      expect(after.at(Square.parse('d4')),
          const Piece(PieceType.rami, Side.south));
      expect(after.at(Square.parse('d6')), isNull);
    });
  });

  test('only the side to move generates moves', () {
    final state = position({'d4': 'A', 'a7': 'a'}, toMove: Side.north);
    expect(state.legalMoves.every((m) => m.from == Square.parse('a7')), isTrue);
  });

  test('legal moves never land on a friendly piece', () {
    final state = GameState.initial();
    for (final move in state.legalMoves) {
      expect(state.at(move.to)?.side, isNot(Side.south), reason: '$move');
    }
  });

  test('play rejects illegal moves and does not mutate the position', () {
    final state = GameState.initial();
    final before = state.toNotation();
    expect(() => state.play(Move.parse('d1-d2')), throwsArgumentError);
    expect(() => state.play(Move.parse('b1-b5')), throwsArgumentError);
    expect(() => state.play(Move.parse('b1xb4')), throwsArgumentError);
    final after = state.play(Move.parse('b1-b4'));
    expect(state.toNotation(), before);
    expect(after.toNotation(), '1fjajf1/2jrj2/7/1F5/7/2JRJ2/2JAJF1 n 1');
  });
}
