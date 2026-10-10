import 'dart:math';

import 'package:flame/components.dart';
import 'package:flame/events.dart';
import 'package:flame/game.dart';
import 'package:flutter/material.dart';
import 'package:game_core/game_core.dart';

import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/app/theme/brand_tokens.g.dart';
import 'package:qala/core/widgets/board/board_view_model.dart';

/// The game board. Draws [model] with Flame and reports taps.
class QalaBoard extends StatefulWidget {
  const new({
    required this.model,
    required this.onSquareTap,
    required this.glyph,
    super.key,
  });

  final BoardViewModel model;
  final ValueChanged<Square> onSquareTap;

  /// Letter drawn on each piece (localized).
  final String Function(PieceType type) glyph;

  @override
  State<QalaBoard> createState() => _QalaBoardState();
}

class _QalaBoardState extends State<QalaBoard> {
  late final ValueNotifier<BoardViewModel> _model = ValueNotifier(widget.model);
  late final _BoardGame _game = _BoardGame(
    model: _model,
    onTap: (s) => widget.onSquareTap(s),
    glyph: (t) => widget.glyph(t),
  );

  @override
  void didUpdateWidget(covariant QalaBoard oldWidget) {
    super.didUpdateWidget(oldWidget);
    _model.value = widget.model;
  }

  @override
  void dispose() {
    _model.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    _game
      ..tokens = context.tokens
      ..reduceMotion = MediaQuery.of(context).disableAnimations;
    return Semantics(
      label: 'Board',
      child: Directionality(
        textDirection: TextDirection.ltr,
        child: AspectRatio(aspectRatio: 1, child: GameWidget(game: _game)),
      ),
    );
  }
}

class _BoardGame extends FlameGame {
  new({required this.model, required this.onTap, required this.glyph});

  final ValueNotifier<BoardViewModel> model;
  final ValueChanged<Square> onTap;
  final String Function(PieceType) glyph;
  AppTokens tokens = AppTokens.light;
  bool reduceMotion = false;

  @override
  Color backgroundColor() => const Color(0x00000000);

  @override
  Future<void> onLoad() async {
    add(_BoardComponent(this)..size = size);
  }

  @override
  void onGameResize(Vector2 size) {
    super.onGameResize(size);
    for (final c in children.whereType<_BoardComponent>()) {
      c.size = size;
    }
  }
}

class _BoardComponent extends PositionComponent with TapCallbacks {
  new(this.game);

  final _BoardGame game;
  Move? _animated;
  double _t = 1;
  final Map<String, TextPainter> _text = {};

  static const _slide = 0.18;
  static const _shot = 0.45;

  double get cell => size.x / boardSize;
  BoardViewModel get model => game.model.value;
  AppTokens get tk => game.tokens;

  @override
  void update(double dt) {
    final last = model.lastMove;
    if (!identical(last, _animated)) {
      _animated = last;
      _t = game.reduceMotion ? 10 : 0;
    }
    _t += dt;
  }

  Offset _center(Square s) {
    final col = model.flipped ? boardSize - 1 - s.file : s.file;
    final row = model.flipped ? s.rank : boardSize - 1 - s.rank;
    return Offset((col + .5) * cell, (row + .5) * cell);
  }

  Rect _rect(Square s) =>
      Rect.fromCenter(center: _center(s), width: cell, height: cell);

  @override
  void onTapDown(TapDownEvent event) {
    final col = (event.localPosition.x / cell).floor();
    final row = (event.localPosition.y / cell).floor();
    if (col < 0 || col >= boardSize || row < 0 || row >= boardSize) return;
    final file = model.flipped ? boardSize - 1 - col : col;
    final rank = model.flipped ? row : boardSize - 1 - row;
    game.onTap(Square(file, rank));
  }

  @override
  void render(Canvas canvas) {
    final state = model.position;
    for (var i = 0; i < squareCount; i++) {
      final s = Square.fromIndex(i);
      final r = _rect(s);
      canvas.drawRect(
        r,
        Paint()
          ..color = (s.file + s.rank).isEven ? tk.boardLight : tk.boardDark,
      );
      if (s == state.rules.southQala || s == state.rules.northQala) {
        final owner = s == state.rules.southQala ? tk.qalaSouth : tk.qalaNorth;
        canvas.drawRRect(
          RRect.fromRectAndRadius(
            r.deflate(cell * .08),
            Radius.circular(cell * .08),
          ),
          Paint()..color = owner,
        );
      } else if (state.rules.wells.contains(s)) {
        canvas
          ..drawCircle(r.center, cell * .38, Paint()..color = tk.boardEdge)
          ..drawCircle(r.center, cell * .30, Paint()..color = tk.water);
      }
      if (model.hintSquares.contains(s)) {
        canvas.drawRect(
          r.deflate(cell * .04),
          Paint()
            ..style = PaintingStyle.stroke
            ..strokeWidth = cell * .06
            ..color = tk.water,
        );
      }
    }
    final last = model.lastMove;
    if (last != null) {
      final p = Paint()..color = tk.selection.withValues(alpha: .3);
      canvas
        ..drawRect(_rect(last.from), p)
        ..drawRect(_rect(last.to), p);
    }
    final sel = model.selected;
    if (sel != null) {
      canvas.drawRect(
        _rect(sel).deflate(cell * .03),
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = cell * .06
          ..color = tk.selection,
      );
    }
    _drawPieces(canvas, state);
    _drawShot(canvas);
    for (final m in model.targets) {
      final c = _center(m.to);
      switch (m.kind) {
        case MoveKind.step:
          canvas.drawCircle(
            c,
            cell * .12,
            Paint()..color = tk.boardEdge.withValues(alpha: .7),
          );
        case MoveKind.capture:
          canvas.drawCircle(
            c,
            cell * .44,
            Paint()
              ..style = PaintingStyle.stroke
              ..strokeWidth = cell * .07
              ..color = tk.capture,
          );
        case MoveKind.shot:
          _crosshair(canvas, c, tk.capture);
      }
    }
    canvas.drawRect(
      Rect.fromLTWH(0, 0, size.x, size.y),
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = cell * .04
        ..color = tk.boardEdge,
    );
  }

  void _drawPieces(Canvas canvas, GameState state) {
    final anim = _animated;
    final sliding = anim != null && anim.kind != MoveKind.shot && _t < _slide;
    for (final (square, piece) in state.pieces()) {
      var c = _center(square);
      if (sliding && square == anim.to) {
        final t = 1 - pow(1 - (_t / _slide).clamp(0, 1), 3).toDouble();
        c = Offset.lerp(_center(anim.from), c, t)!;
      }
      _piece(canvas, c, piece, supplied: state.isSupplied(square));
    }
  }

  void _piece(Canvas canvas, Offset c, Piece piece, {required bool supplied}) {
    final r = cell * .34;
    final alpha = supplied ? 1.0 : .5;
    final south = piece.side == Side.south;
    if (supplied && piece.type != PieceType.amir) {
      canvas.drawCircle(
        c,
        r * 1.22,
        Paint()
          ..color = tk.supplyGlow.withValues(alpha: .6)
          ..maskFilter = MaskFilter.blur(BlurStyle.normal, cell * .05),
      );
    }
    final path = switch (piece.type) {
      PieceType.amir => Path()..addOval(Rect.fromCircle(center: c, radius: r)),
      PieceType.jundi =>
        Path()..addRRect(
          RRect.fromRectAndRadius(
            Rect.fromCircle(center: c, radius: r * .86),
            Radius.circular(r * .25),
          ),
        ),
      PieceType.faris =>
        Path()
          ..moveTo(c.dx, c.dy - r * 1.05)
          ..lineTo(c.dx + r, c.dy + r * .8)
          ..lineTo(c.dx - r, c.dy + r * .8)
          ..close(),
      PieceType.rami =>
        Path()
          ..moveTo(c.dx, c.dy - r * 1.08)
          ..lineTo(c.dx + r * .95, c.dy)
          ..lineTo(c.dx, c.dy + r * 1.08)
          ..lineTo(c.dx - r * .95, c.dy)
          ..close(),
    };
    final edge = (south ? tk.southEdge : tk.northEdge).withValues(alpha: alpha);
    canvas
      ..drawPath(
        path,
        Paint()
          ..color = (south ? tk.southFill : tk.northFill).withValues(
            alpha: alpha,
          ),
      )
      ..drawPath(
        path,
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = cell * .045
          ..color = edge,
      );
    if (piece.type == PieceType.amir) {
      canvas.drawCircle(
        c,
        r * .78,
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = cell * .03
          ..color = edge,
      );
    }
    final g = game.glyph(piece.type);
    final key = '$g${piece.side.name}$alpha${cell.round()}';
    final tp = _text.putIfAbsent(
      key,
      () => TextPainter(
        text: TextSpan(
          text: g,
          style: TextStyle(
            // Plex Arabic covers both the Arabic and the Latin letters.
            fontFamily: BrandFonts.arabic,
            fontSize: cell * .32,
            fontWeight: FontWeight.w700,
            color: (south ? tk.southEdge : tk.southFill).withValues(
              alpha: alpha,
            ),
          ),
        ),
        textDirection: TextDirection.ltr,
      )..layout(),
    );
    final dy = piece.type == PieceType.faris ? cell * .06 : 0.0;
    tp.paint(canvas, c + Offset(-tp.width / 2, -tp.height / 2 + dy));
    if (!supplied) {
      final m = c + Offset(r * .8, -r * .8);
      final s = cell * .1;
      canvas
        ..drawCircle(m, s * 1.6, Paint()..color = const Color(0xFFFFFFFF))
        ..drawCircle(m, s * .8, Paint()..color = tk.water)
        ..drawLine(
          m + Offset(-s * 1.1, s * 1.1),
          m + Offset(s * 1.1, -s * 1.1),
          Paint()
            ..strokeWidth = s * .35
            ..color = tk.capture,
        );
    }
  }

  void _drawShot(Canvas canvas) {
    final m = _animated;
    if (m == null || m.kind != MoveKind.shot || _t > _shot) return;
    final t = (_t / _shot).clamp(0.0, 1.0);
    final a = _center(m.from);
    final b = _center(m.to);
    canvas.drawLine(
      a,
      Offset.lerp(a, b, min(1, t * 2))!,
      Paint()
        ..strokeWidth = cell * .06
        ..color = tk.capture.withValues(alpha: 1 - t),
    );
  }

  void _crosshair(Canvas canvas, Offset c, Color color) {
    final p = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = cell * .05
      ..color = color;
    canvas.drawCircle(c, cell * .34, p);
    for (final (dx, dy) in const [(1, 0), (-1, 0), (0, 1), (0, -1)]) {
      canvas.drawLine(
        c + Offset(dx * cell * .2, dy * cell * .2),
        c + Offset(dx * cell * .46, dy * cell * .46),
        p,
      );
    }
  }
}
