import 'package:flutter/widgets.dart';
import 'package:game_core/game_core.dart';

/// The letter drawn on a piece, in the UI language.
String pieceGlyph(BuildContext context, PieceType type) {
  final ar = Localizations.localeOf(context).languageCode == 'ar';
  return switch (type) {
    PieceType.amir => ar ? 'أ' : 'A',
    PieceType.jundi => ar ? 'ج' : 'J',
    PieceType.faris => ar ? 'ف' : 'F',
    PieceType.rami => ar ? 'ر' : 'R',
  };
}
