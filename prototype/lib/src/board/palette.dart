import 'dart:ui';

import 'package:game_core/game_core.dart';

/// Prototype colours. Simple, high-contrast, no art (the brand kit comes in
/// step 5).
abstract final class Palette {
  static const background = Color(0xFFF6EBD6);
  static const squareLight = Color(0xFFEFD9B0);
  static const squareDark = Color(0xFFE2C291);
  static const boardEdge = Color(0xFF8A6235);
  static const coordinates = Color(0x998A6235);

  static const water = Color(0xFF2F8FD0);
  static const waterLight = Color(0xFFBFE4FA);
  static const wellRing = Color(0xFF8A6235);
  static const supplyGlow = Color(0xAA4FC3F7);
  static const dryBadge = Color(0xFFFFFFFF);

  static const lastMove = Color(0x55F2C14E);
  static const selection = Color(0xFFE2A400);
  static const target = Color(0x99403020);
  static const capture = Color(0xFFD64541);

  static const sandFill = Color(0xFFFFF6E0);
  static const sandEdge = Color(0xFF7A4E1D);
  static const indigoFill = Color(0xFF2F3B8F);
  static const indigoEdge = Color(0xFF111A4F);

  static Color pieceFill(Side side) =>
      side == Side.south ? sandFill : indigoFill;
  static Color pieceEdge(Side side) =>
      side == Side.south ? sandEdge : indigoEdge;
  static Color pieceText(Side side) =>
      side == Side.south ? sandEdge : const Color(0xFFF6EBD6);

  static Color qala(Side side) =>
      side == Side.south ? const Color(0xFFC9A46A) : const Color(0xFF8C93C2);
}
