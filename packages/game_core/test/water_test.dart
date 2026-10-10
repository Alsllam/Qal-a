import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

const rules = RuleSet(version: 'test', waterToWin: 3);

GameState pos(String text) => GameState.fromNotation(text, rules: rules);

void main() {
  test('water is off in v0.1 and absent from notation', () {
    final state = GameState.initial(RuleSet.v0_1).play(Move.parse('b1-b4'));
    expect(state.water(Side.south), 0);
    expect(state.toNotation().split(' '), hasLength(3));
  });

  test('notation carries water points and round-trips', () {
    final state = pos('3a3/7/7/2J4/7/7/3A3 n 7 2:1');
    expect(state.water(Side.south), 2);
    expect(state.water(Side.north), 1);
    expect(state.toNotation(), '3a3/7/7/2J4/7/7/3A3 n 7 2:1');
    expect(() => pos('3a3/7/7/7/7/7/3A3 s 0 2'), throwsFormatException);
  });

  test('a side gains one point per Well it holds at the start of its turn', () {
    // South holds c4. North holds e4.
    var state = pos('3a3/7/7/2J1j2/7/7/3A3 s 0 0:0');
    state = state.play(Move.parse('d1-d2')); // start of North's turn
    expect(state.water(Side.north), 1);
    expect(state.water(Side.south), 0);
    state = state.play(Move.parse('d7-d6')); // start of South's turn
    expect(state.water(Side.south), 1);
    expect(state.water(Side.north), 1);
  });

  test('a Well lost during the opponent\'s turn gives nothing', () {
    // North Faris on e4 (Well, supplied) captures the South Jundi on c4.
    var state = pos('3a3/7/7/2J1f2/7/7/3A3 n 0 0:0');
    state = state.play(Move.parse('e4xc4'));
    expect(state.water(Side.south), 0);
  });

  test('reaching the target wins at the start of your turn', () {
    final state = pos('3a3/7/7/2J4/7/7/3A3 n 9 2:0');
    final after = state.play(Move.parse('d7-d6'));
    expect(after.water(Side.south), 3);
    expect(after.outcome, const Outcome(Side.south, EndReason.waterVictory));
  });

  test('the mover\'s own win comes first', () {
    // South would reach 3 water, but North captures the South Amir first.
    final state = pos('7/7/7/2J4/7/3a3/3A3 n 9 2:0');
    final after = state.play(Move.parse('d2xd1'));
    expect(after.outcome, const Outcome(Side.north, EndReason.amirCaptured));
  });

  test('at the ply limit, more water wins before Wells and pieces', () {
    const limited = RuleSet(version: 'test', waterToWin: 30, plyLimit: 10);
    final state = GameState.fromNotation(
      '3a3/7/7/4j2/7/7/3A3 s 9 5:4',
      rules: limited,
    );
    // North holds a Well and gains 1 (5:5)... then Wells decide.
    final after = state.play(Move.parse('d1-d2'));
    expect(after.water(Side.north), 5);
    expect(after.outcome, const Outcome(Side.north, EndReason.plyLimitWells));

    final ahead = GameState.fromNotation(
      '3a3/7/7/7/7/7/3A3 s 9 5:4',
      rules: limited,
    ).play(Move.parse('d1-d2'));
    expect(ahead.outcome, const Outcome(Side.south, EndReason.plyLimitWater));
  });

  test('copyWith can turn water on and off', () {
    final on = RuleSet.standard.copyWith(waterToWin: () => 8);
    expect(on.waterToWin, 8);
    expect(on.copyWith(waterToWin: () => null).waterToWin, isNull);
    expect(on.copyWith(plyLimit: 40).waterToWin, 8);
  });

  test('North can start with water points', () {
    final state = GameState.initial(rules.copyWith(northStartWater: 1));
    expect(state.water(Side.north), 1);
    expect(state.water(Side.south), 0);
  });

  test('with waterNeedsSupply, an unsupplied Well holder earns nothing', () {
    final strict =
        rules.copyWith(wellsAreSources: false, waterNeedsSupply: true);
    // South Jundi on c4 is alone: no Qal'a, no Amir nearby.
    var state =
        GameState.fromNotation('3a3/7/7/2J4/7/7/A6 n 0 0:0', rules: strict);
    expect(state.isSupplied(Square.parse('c4')), isFalse);
    state = state.play(Move.parse('d7-d6'));
    expect(state.water(Side.south), 0);
    // With the Amir next to it, it is supplied and scores.
    state =
        GameState.fromNotation('3a3/7/7/2J4/1A5/7/7 n 0 0:0', rules: strict);
    expect(state.play(Move.parse('d7-d6')).water(Side.south), 1);
  });

  test('when Wells are not sources, a piece on a Well is not supplied', () {
    final noSource = rules.copyWith(wellsAreSources: false);
    final state = GameState.fromNotation('3a3/7/7/2J4/7/7/A6', rules: noSource);
    expect(state.isSupplied(Square.parse('c4')), isFalse);
  });

  test('matches the water example in docs/rules.md §7', () {
    // c4 linked via c3 to the Amir on d2; e4 cut off. North to move.
    final state = GameState.fromNotation('3a3/7/7/2J1J2/2J4/3A3/7 n 0 0:0');
    final after = state.play(Move.parse('d7-d6'));
    expect(after.water(Side.south), 1);
  });

  test('an Amir on a Well earns nothing when amirEarnsWater is off', () {
    const text = '3a3/7/7/2A4/7/7/7 n 0 0:0';
    final on = GameState.fromNotation(text, rules: rules);
    expect(on.play(Move.parse('d7-d6')).water(Side.south), 1);
    final off = GameState.fromNotation(text,
        rules: rules.copyWith(amirEarnsWater: false));
    expect(off.play(Move.parse('d7-d6')).water(Side.south), 0);
  });
}
