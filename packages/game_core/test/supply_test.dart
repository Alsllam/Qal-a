import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

import 'helpers.dart';

void main() {
  const noAmirSource = RuleSet(version: 'test', amirIsSource: false);

  test('every piece is supplied in the opening position', () {
    final state = GameState.initial();
    for (final side in Side.values) {
      expect(state.suppliedSquares(side), hasLength(8));
    }
  });

  test('supply flows along a chain of touching friendly pieces', () {
    final state = position(
        {'d1': 'A', 'd2': 'J', 'e3': 'J', 'e4': 'F', 'g4': 'J', 'a7': 'a'});
    expect(supplied(state, Side.south), {'d1', 'd2', 'e3', 'e4'});
    expect(state.isSupplied(Square.parse('g4')), isFalse);
  });

  test('matches the supply example in docs/rules.md §6', () {
    final state = position(
      {
        'g5': 'J',
        'e4': 'F',
        'e3': 'J',
        'd2': 'J',
        'b3': 'J',
        'd1': 'A',
        'a7': 'a'
      },
    );
    expect(supplied(state, Side.south), {'e4', 'e3', 'd2', 'd1'});
  });

  test('diagonal contact counts as touching', () {
    final state = position({'b3': 'A', 'c4': 'J', 'd5': 'J', 'a7': 'a'});
    expect(supplied(state, Side.south), {'b3', 'c4', 'd5'});
  });

  test('pieces on or next to their own Qal\'a are supplied', () {
    final state = position(
      {'a4': 'A', 'd1': 'J', 'c2': 'J', 'e2': 'J', 'd3': 'J', 'a7': 'a'},
      rules: noAmirSource,
    );
    // d3 touches c2 and e2, so it is supplied through them.
    expect(supplied(state, Side.south), {'d1', 'c2', 'e2', 'd3'});
  });

  test('the Qal\'a stops giving water while an enemy stands on it', () {
    final state = position(
      {'a4': 'A', 'c2': 'J', 'd1': 'j', 'g7': 'a'},
      rules: noAmirSource,
    );
    expect(supplied(state, Side.south), isEmpty);
  });

  test('in v0.1 a piece on a Well is a source, and so is its chain', () {
    final state = position(
      {'a1': 'A', 'c4': 'J', 'b5': 'F', 'a6': 'J', 'g7': 'a'},
      rules: noAmirSource,
    );
    expect(supplied(state, Side.south), {'c4', 'b5', 'a6'});
  });

  test('an empty or enemy-held Well gives no water', () {
    final state = position(
      {'a1': 'A', 'c4': 'j', 'c5': 'J', 'd4': 'J', 'g7': 'a'},
      rules: noAmirSource,
    );
    expect(supplied(state, Side.south), isEmpty);
  });

  test('the Amir is a source unless the rule set turns it off', () {
    final pieces = {'a5': 'A', 'b5': 'J', 'g7': 'a'};
    expect(supplied(position(pieces), Side.south), {'a5', 'b5'});
    expect(
        supplied(position(pieces, rules: noAmirSource), Side.south), isEmpty);
  });

  test('enemy pieces never relay supply', () {
    final state = position({'d1': 'A', 'd2': 'j', 'd3': 'J', 'a7': 'a'});
    expect(state.isSupplied(Square.parse('d3')), isFalse);
  });

  test('empty squares are never supplied', () {
    expect(GameState.initial().isSupplied(Square.parse('d4')), isFalse);
  });

  test('cutting a chain removes supply after the move', () {
    // South chain d1-d2-d3-d4; North Jundi on c3 captures d3... by stepping.
    final state = position(
      {
        'd1': 'A',
        'd2': 'J',
        'd3': 'J',
        'e4': 'J',
        'c3': 'j',
        'c4': 'j',
        'g7': 'a'
      },
      toMove: Side.north,
      rules: RuleSet.v0_1,
    );
    // North Jundi c3 is supplied by the c4 Well.
    final after = state.play(Move.parse('c3xd3'));
    expect(after.isSupplied(Square.parse('e4')), isTrue,
        reason: 'e4 is a Well');
    final state2 = position(
      {
        'd1': 'A',
        'd2': 'J',
        'd3': 'J',
        'd4': 'J',
        'd5': 'J',
        'c3': 'j',
        'c4': 'j',
        'g7': 'a'
      },
      toMove: Side.north,
      rules: RuleSet.v0_1,
    );
    expect(state2.isSupplied(Square.parse('d5')), isTrue);
    final cut = state2.play(Move.parse('c3xd3'));
    expect(cut.isSupplied(Square.parse('d4')), isFalse);
    expect(cut.isSupplied(Square.parse('d5')), isFalse);
    expect(cut.isSupplied(Square.parse('d2')), isTrue);
  });
}
