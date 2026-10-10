import 'dart:math';
import 'dart:ui';

import 'package:flame/components.dart';
import 'package:flame/events.dart';
import 'package:flame/game.dart';
import 'package:flutter/painting.dart' show TextPainter, TextSpan, TextStyle;
import 'package:game_core/game_core.dart';

import '../match_controller.dart';
import '../strings.dart';
import 'palette.dart';

/// Flame game that draws the board and turns taps into [MatchController.tap].
///
/// Drawing is immediate-mode from the controller's state, so the board can
/// never drift out of sync with the rules engine. Only the last move is
/// animated.
class BoardGame extends FlameGame {
  BoardGame({
    required this.controller,
    required this.strings,
    this.flipped = false,
  });

  final MatchController controller;
  final S strings;

  /// Draw North at the bottom (when the human plays North against the AI).
  final bool flipped;

  late final BoardComponent board;

  @override
  Color backgroundColor() => Palette.background;

  @override
  Future<void> onLoad() async {
    board = BoardComponent(
      controller: controller,
      strings: strings,
      flipped: flipped,
    );
    add(board);
    _layout(size);
  }

  @override
  void onGameResize(Vector2 size) {
    super.onGameResize(size);
    if (isLoaded) _layout(size);
  }

  void _layout(Vector2 size) {
    final side = min(size.x, size.y);
    board
      ..size = Vector2.all(side)
      ..position = Vector2((size.x - side) / 2, (size.y - side) / 2);
  }
}

/// The 7×7 board with its pieces.
class BoardComponent extends PositionComponent with TapCallbacks {
  BoardComponent({
    required this.controller,
    required this.strings,
    required this.flipped,
  });

  final MatchController controller;
  final S strings;
  final bool flipped;

  static const _moveSeconds = 0.18;
  static const _shotSeconds = 0.45;

  Move? _animatedMove;
  double _animTime = 1;
  final Map<String, TextPainter> _glyphCache = {};

  double get cell => size.x / boardSize;

  @override
  void update(double dt) {
    final last = controller.lastMove;
    if (!identical(last, _animatedMove)) {
      _animatedMove = last;
      _animTime = 0;
    }
    _animTime += dt;
  }

  // ---------------------------------------------------------------- geometry

  Offset _center(Square s) {
    final col = flipped ? boardSize - 1 - s.file : s.file;
    final row = flipped ? s.rank : boardSize - 1 - s.rank;
    return Offset((col + 0.5) * cell, (row + 0.5) * cell);
  }

  Rect _rect(Square s) =>
      Rect.fromCenter(center: _center(s), width: cell, height: cell);

  Square? squareAt(Vector2 local) {
    final col = (local.x / cell).floor();
    final row = (local.y / cell).floor();
    if (col < 0 || col >= boardSize || row < 0 || row >= boardSize) return null;
    final file = flipped ? boardSize - 1 - col : col;
    final rank = flipped ? row : boardSize - 1 - row;
    return Square(file, rank);
  }

  @override
  void onTapDown(TapDownEvent event) {
    final square = squareAt(event.localPosition);
    if (square != null) controller.tap(square);
  }

  // ------------------------------------------------------------------ render

  @override
  void render(Canvas canvas) {
    final state = controller.state;
    _drawSquares(canvas, state);
    _drawHighlights(canvas, state);
    _drawPieces(canvas, state);
    _drawShot(canvas);
    _drawTargets(canvas, state);
    _drawCoordinates(canvas);
  }

  void _drawSquares(Canvas canvas, GameState state) {
    final rules = state.rules;
    for (var i = 0; i < squareCount; i++) {
      final s = Square.fromIndex(i);
      final rect = _rect(s);
      final light = (s.file + s.rank).isEven;
      canvas.drawRect(
        rect,
        Paint()..color = light ? Palette.squareLight : Palette.squareDark,
      );

      if (s == rules.southQala || s == rules.northQala) {
        _drawQala(canvas, rect, s == rules.southQala ? Side.south : Side.north);
      } else if (rules.wells.contains(s)) {
        _drawWell(canvas, rect);
      }
    }
    canvas.drawRect(
      Rect.fromLTWH(0, 0, size.x, size.y),
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = cell * 0.04
        ..color = Palette.boardEdge,
    );
  }

  void _drawQala(Canvas canvas, Rect rect, Side owner) {
    final inner = rect.deflate(cell * 0.08);
    final paint = Paint()..color = Palette.qala(owner);
    canvas.drawRect(inner, paint);
    // Crenellations along the outer edge.
    final merlon = inner.width / 7;
    final top = owner == Side.south ? !flipped : flipped;
    for (var k = 0; k < 7; k += 2) {
      final x = inner.left + k * merlon;
      final y = top ? inner.bottom - merlon * 0.2 : inner.top - merlon * 0.8;
      canvas.drawRect(Rect.fromLTWH(x, y, merlon, merlon), paint);
    }
  }

  void _drawWell(Canvas canvas, Rect rect) {
    final c = rect.center;
    canvas.drawCircle(c, cell * 0.38, Paint()..color = Palette.wellRing);
    canvas.drawCircle(c, cell * 0.30, Paint()..color = Palette.water);
    final wave = Paint()
      ..color = Palette.waterLight
      ..style = PaintingStyle.stroke
      ..strokeWidth = cell * 0.03;
    for (final dy in [-0.07, 0.07]) {
      final path = Path()..moveTo(c.dx - cell * 0.16, c.dy + cell * dy);
      path.quadraticBezierTo(
        c.dx - cell * 0.08,
        c.dy + cell * (dy - 0.05),
        c.dx,
        c.dy + cell * dy,
      );
      path.quadraticBezierTo(
        c.dx + cell * 0.08,
        c.dy + cell * (dy + 0.05),
        c.dx + cell * 0.16,
        c.dy + cell * dy,
      );
      canvas.drawPath(path, wave);
    }
  }

  void _drawHighlights(Canvas canvas, GameState state) {
    final last = controller.lastMove;
    if (last != null) {
      final paint = Paint()..color = Palette.lastMove;
      canvas.drawRect(_rect(last.from), paint);
      canvas.drawRect(_rect(last.to), paint);
    }
    final selected = controller.selected;
    if (selected != null) {
      canvas.drawRect(
        _rect(selected).deflate(cell * 0.03),
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = cell * 0.06
          ..color = Palette.selection,
      );
    }
  }

  void _drawTargets(Canvas canvas, GameState state) {
    for (final move in controller.selectedMoves) {
      final c = _center(move.to);
      switch (move.kind) {
        case MoveKind.step:
          canvas.drawCircle(c, cell * 0.12, Paint()..color = Palette.target);
        case MoveKind.capture:
          canvas.drawCircle(
            c,
            cell * 0.44,
            Paint()
              ..style = PaintingStyle.stroke
              ..strokeWidth = cell * 0.07
              ..color = Palette.capture,
          );
        case MoveKind.shot:
          _drawCrosshair(canvas, c, Palette.capture);
      }
    }
  }

  void _drawCrosshair(Canvas canvas, Offset c, Color color) {
    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = cell * 0.05
      ..color = color;
    canvas.drawCircle(c, cell * 0.34, paint);
    final r = cell * 0.46;
    final r0 = cell * 0.2;
    for (final (dx, dy) in [(1, 0), (-1, 0), (0, 1), (0, -1)]) {
      canvas.drawLine(
        c + Offset(dx * r0, dy * r0),
        c + Offset(dx * r, dy * r),
        paint,
      );
    }
  }

  void _drawShot(Canvas canvas) {
    final move = _animatedMove;
    if (move == null ||
        move.kind != MoveKind.shot ||
        _animTime > _shotSeconds) {
      return;
    }
    final t = (_animTime / _shotSeconds).clamp(0.0, 1.0);
    final from = _center(move.from);
    final to = _center(move.to);
    final paint = Paint()
      ..strokeWidth = cell * 0.06
      ..color = Palette.capture.withValues(alpha: 1 - t);
    canvas.drawLine(from, Offset.lerp(from, to, min(1, t * 2))!, paint);
    _drawCrosshair(canvas, to, Palette.capture.withValues(alpha: 1 - t));
  }

  void _drawPieces(Canvas canvas, GameState state) {
    final animating =
        _animatedMove != null &&
        _animatedMove!.kind != MoveKind.shot &&
        _animTime < _moveSeconds;
    for (final (square, piece) in state.pieces()) {
      var center = _center(square);
      if (animating && square == _animatedMove!.to) {
        final t = Curves.easeOut(_animTime / _moveSeconds);
        center = Offset.lerp(_center(_animatedMove!.from), center, t)!;
      }
      _drawPiece(canvas, center, piece, supplied: state.isSupplied(square));
    }
  }

  void _drawPiece(
    Canvas canvas,
    Offset c,
    Piece piece, {
    required bool supplied,
  }) {
    final r = cell * 0.34;
    final opacity = supplied ? 1.0 : 0.5;

    if (supplied && piece.type != PieceType.amir) {
      canvas.drawCircle(
        c,
        r * 1.22,
        Paint()
          ..color = Palette.supplyGlow
          ..maskFilter = MaskFilter.blur(BlurStyle.normal, cell * 0.05),
      );
    }

    final fill = Paint()
      ..color = Palette.pieceFill(piece.side).withValues(alpha: opacity);
    final stroke = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = cell * 0.045
      ..color = Palette.pieceEdge(piece.side).withValues(alpha: opacity);
    final path = _shape(piece.type, c, r);
    canvas.drawPath(path, fill);
    canvas.drawPath(path, stroke);
    if (piece.type == PieceType.amir) {
      canvas.drawCircle(c, r * 0.78, stroke);
    }

    _drawGlyph(canvas, c, piece, opacity);

    if (!supplied) _drawDryMark(canvas, c + Offset(r * 0.8, -r * 0.8));
  }

  Path _shape(PieceType type, Offset c, double r) {
    switch (type) {
      case PieceType.amir:
        return Path()..addOval(Rect.fromCircle(center: c, radius: r));
      case PieceType.jundi:
        return Path()..addRRect(
          RRect.fromRectAndRadius(
            Rect.fromCircle(center: c, radius: r * 0.86),
            Radius.circular(r * 0.25),
          ),
        );
      case PieceType.faris:
        return Path()
          ..moveTo(c.dx, c.dy - r * 1.05)
          ..lineTo(c.dx + r * 1.0, c.dy + r * 0.8)
          ..lineTo(c.dx - r * 1.0, c.dy + r * 0.8)
          ..close();
      case PieceType.rami:
        return Path()
          ..moveTo(c.dx, c.dy - r * 1.08)
          ..lineTo(c.dx + r * 0.95, c.dy)
          ..lineTo(c.dx, c.dy + r * 1.08)
          ..lineTo(c.dx - r * 0.95, c.dy)
          ..close();
    }
  }

  void _drawGlyph(Canvas canvas, Offset c, Piece piece, double opacity) {
    final glyph = strings.pieceGlyph(piece.type);
    final key = '$glyph${piece.side.name}$opacity${cell.round()}';
    final painter = _glyphCache.putIfAbsent(key, () {
      return TextPainter(
        text: TextSpan(
          text: glyph,
          style: TextStyle(
            fontSize: cell * 0.32,
            fontWeight: FontWeight.w700,
            color: Palette.pieceText(piece.side).withValues(alpha: opacity),
          ),
        ),
        textDirection: TextDirection.ltr,
      )..layout();
    });
    final dy = piece.type == PieceType.faris ? cell * 0.06 : 0.0;
    painter.paint(
      canvas,
      c + Offset(-painter.width / 2, -painter.height / 2 + dy),
    );
  }

  /// Small crossed-out drop: "this piece has no water".
  void _drawDryMark(Canvas canvas, Offset c) {
    final s = cell * 0.1;
    final drop = Path()
      ..moveTo(c.dx, c.dy - s * 1.3)
      ..quadraticBezierTo(c.dx + s * 1.1, c.dy, c.dx, c.dy + s)
      ..quadraticBezierTo(c.dx - s * 1.1, c.dy, c.dx, c.dy - s * 1.3)
      ..close();
    canvas.drawCircle(c, s * 1.6, Paint()..color = Palette.dryBadge);
    canvas.drawPath(drop, Paint()..color = Palette.water);
    canvas.drawLine(
      c + Offset(-s * 1.1, s * 1.1),
      c + Offset(s * 1.1, -s * 1.1),
      Paint()
        ..strokeWidth = s * 0.35
        ..color = Palette.capture,
    );
  }

  void _drawCoordinates(Canvas canvas) {
    final style = TextStyle(fontSize: cell * 0.14, color: Palette.coordinates);
    for (var i = 0; i < boardSize; i++) {
      final file = Square(i, flipped ? boardSize - 1 : 0);
      final rank = Square(flipped ? boardSize - 1 : 0, i);
      _label(
        canvas,
        String.fromCharCode(0x61 + i),
        _rect(file).bottomRight + Offset(-cell * 0.14, -cell * 0.2),
        style,
      );
      _label(
        canvas,
        '${i + 1}',
        _rect(rank).topLeft + Offset(cell * 0.05, cell * 0.03),
        style,
      );
    }
  }

  void _label(Canvas canvas, String text, Offset at, TextStyle style) {
    final key = 'label$text${cell.round()}';
    final painter = _glyphCache.putIfAbsent(
      key,
      () => TextPainter(
        text: TextSpan(text: text, style: style),
        textDirection: TextDirection.ltr,
      )..layout(),
    );
    painter.paint(canvas, at);
  }
}

/// Minimal easing used for piece slides.
abstract final class Curves {
  static double easeOut(double t) {
    final x = t.clamp(0.0, 1.0);
    return 1 - pow(1 - x, 3).toDouble();
  }
}
