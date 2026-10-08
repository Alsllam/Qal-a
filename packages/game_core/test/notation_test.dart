import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

import 'helpers.dart';

void main() {
  test('initial position matches the standard setup', () {
    final state = GameState.initial();
    expect(state.toNotation(), '1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0');
    expect(state.toMove, Side.south);
    expect(state.ply, 0);
    expect(state.isOver, isFalse);
    expect(state.pieceCount(Side.south), 8);
    expect(state.pieceCount(Side.north), 8);
    expect(state.amirSquare(Side.south), Square.parse('d1'));
    expect(state.amirSquare(Side.north), Square.parse('d7'));
  });

  test('the opening position is mirror-symmetric', () {
    final state = GameState.initial();
    for (final (square, piece) in state.pieces(Side.south)) {
      final mirror = Square(square.file, boardSize - 1 - square.rank);
      expect(state.at(mirror), Piece(piece.type, Side.north));
    }
  });

  test('notation round-trips', () {
    const text = '3a3/7/2R4/1F1j3/7/7/3A3 n 17';
    final state = GameState.fromNotation(text);
    expect(state.toNotation(), text);
    expect(GameState.fromNotation(state.toNotation()), state);
  });

  test('side to move and ply default to South and 0', () {
    final state = GameState.fromNotation('3a3/7/7/7/7/7/3A3');
    expect(state.toMove, Side.south);
    expect(state.ply, 0);
  });

  test('rejects malformed positions', () {
    for (final bad in [
      '3a3/7/7/7/7/3A3', // 6 ranks
      '3a3/7/7/7/7/7/3A4', // 8 files
      '3a3/7/7/7/7/7/3A2', // 6 files
      '7/7/7/7/7/7/3A3', // no North Amir
      '3a3/7/7/7/7/7/2AA2', // two South Amirs
      '3a3/7/7/7/7/7/3K3', // unknown piece
      '3a3/7/7/7/7/7/3A3 x 0', // unknown side
    ]) {
      expect(() => GameState.fromNotation(bad), throwsFormatException,
          reason: bad);
    }
  });

  test('toAscii marks Qal\'a, Wells and unsupplied pieces', () {
    final state = position({'d1': 'A', 'a7': 'a', 'g4': 'J'});
    final ascii = state.toAscii();
    expect(ascii, contains("J'"));
    expect(ascii.split('\n')[1], startsWith('7 | a  .  .  Q'));
    expect(ascii.split('\n')[4], contains('W'));
  });
}
