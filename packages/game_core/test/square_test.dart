import 'package:game_core/game_core.dart';
import 'package:test/test.dart';

void main() {
  group('Square', () {
    test('parse and name round-trip for every square', () {
      for (var i = 0; i < squareCount; i++) {
        final square = Square.fromIndex(i);
        expect(Square.parse(square.name), square);
        expect(square.index, i);
      }
    });

    test('a1 is file 0 rank 0, g7 is file 6 rank 6', () {
      expect(Square.parse('a1'), const Square(0, 0));
      expect(Square.parse('g7'), const Square(6, 6));
      expect(const Square(3, 3).name, 'd4');
    });

    test('rejects squares off the board', () {
      for (final bad in ['h1', 'a8', 'a0', '', 'a', 'd44', 'D4']) {
        expect(() => Square.parse(bad), throwsFormatException, reason: bad);
      }
      expect(() => Square.fromIndex(49), throwsRangeError);
    });

    test('offset returns null off the board', () {
      expect(Square.parse('a1').offset(-1, 0), isNull);
      expect(Square.parse('g7').offset(0, 1), isNull);
      expect(Square.parse('d4').offset(2, -3), Square.parse('f1'));
    });
  });

  group('Move', () {
    test('parse and notation round-trip', () {
      for (final text in ['b1-b4', 'c3xc4', 'd2*d4']) {
        expect(Move.parse(text).notation, text);
      }
      expect(Move.parse('d2*d4').kind, MoveKind.shot);
      expect(Move.parse('d2*d4').removesPiece, isTrue);
      expect(Move.parse('b1-b4').removesPiece, isFalse);
    });

    test('rejects malformed moves', () {
      for (final bad in ['b1b4', 'b1+b4', 'h1-h2', 'b1-b8', '']) {
        expect(() => Move.parse(bad), throwsFormatException, reason: bad);
      }
    });
  });

  group('Piece', () {
    test('letters encode side by case', () {
      expect(Piece.fromLetter('F'), const Piece(PieceType.faris, Side.south));
      expect(Piece.fromLetter('r'), const Piece(PieceType.rami, Side.north));
      expect(const Piece(PieceType.amir, Side.north).letter, 'a');
      expect(() => Piece.fromLetter('K'), throwsFormatException);
    });

    test('side opponent', () {
      expect(Side.south.opponent, Side.north);
      expect(Side.north.opponent, Side.south);
    });
  });
}
