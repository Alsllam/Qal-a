import 'package:flutter/material.dart';

import 'package:qala/app/theme/brand_tokens.g.dart';

/// App-specific design tokens (game colours, water, status) on top of the
/// Material colour scheme. Widgets read `context.tokens.x`, never raw colours.
@immutable
class AppTokens extends ThemeExtension<AppTokens> {
  const new({
    required this.surfaceRaised,
    required this.surfaceSunken,
    required this.textMuted,
    required this.water,
    required this.waterLight,
    required this.success,
    required this.danger,
    required this.boardLight,
    required this.boardDark,
    required this.boardEdge,
    required this.southFill,
    required this.southEdge,
    required this.northFill,
    required this.northEdge,
    required this.qalaSouth,
    required this.qalaNorth,
    required this.supplyGlow,
    required this.selection,
    required this.capture,
  });

  static const light = AppTokens(
    surfaceRaised: BrandLightScheme.surfaceRaised,
    surfaceSunken: BrandLightScheme.surfaceSunken,
    textMuted: BrandLightScheme.textMuted,
    water: BrandPalette.water500,
    waterLight: BrandPalette.water50,
    success: BrandPalette.success500,
    danger: BrandPalette.danger500,
    boardLight: BrandPalette.gameBoardLight,
    boardDark: BrandPalette.gameBoardDark,
    boardEdge: BrandPalette.gameBoardEdge,
    southFill: BrandPalette.gameSouthFill,
    southEdge: BrandPalette.gameSouthEdge,
    northFill: BrandPalette.gameNorthFill,
    northEdge: BrandPalette.gameNorthEdge,
    qalaSouth: BrandPalette.gameQalaSouth,
    qalaNorth: BrandPalette.gameQalaNorth,
    supplyGlow: BrandPalette.gameSupplyGlow,
    selection: BrandPalette.gameSelection,
    capture: BrandPalette.gameCapture,
  );

  static final AppTokens dark = light.copyWith(
    surfaceRaised: BrandDarkScheme.surfaceRaised,
    surfaceSunken: BrandDarkScheme.surfaceSunken,
    textMuted: BrandDarkScheme.textMuted,
    waterLight: const Color(0xFF1D3550),
  );

  final Color surfaceRaised;
  final Color surfaceSunken;
  final Color textMuted;
  final Color water;
  final Color waterLight;
  final Color success;
  final Color danger;
  final Color boardLight;
  final Color boardDark;
  final Color boardEdge;
  final Color southFill;
  final Color southEdge;
  final Color northFill;
  final Color northEdge;
  final Color qalaSouth;
  final Color qalaNorth;
  final Color supplyGlow;
  final Color selection;
  final Color capture;

  @override
  AppTokens copyWith({
    Color? surfaceRaised,
    Color? surfaceSunken,
    Color? textMuted,
    Color? water,
    Color? waterLight,
  }) => AppTokens(
    surfaceRaised: surfaceRaised ?? this.surfaceRaised,
    surfaceSunken: surfaceSunken ?? this.surfaceSunken,
    textMuted: textMuted ?? this.textMuted,
    water: water ?? this.water,
    waterLight: waterLight ?? this.waterLight,
    success: success,
    danger: danger,
    boardLight: boardLight,
    boardDark: boardDark,
    boardEdge: boardEdge,
    southFill: southFill,
    southEdge: southEdge,
    northFill: northFill,
    northEdge: northEdge,
    qalaSouth: qalaSouth,
    qalaNorth: qalaNorth,
    supplyGlow: supplyGlow,
    selection: selection,
    capture: capture,
  );

  @override
  AppTokens lerp(covariant AppTokens? other, double t) =>
      t < 0.5 ? this : (other ?? this);
}

extension AppTokensX on BuildContext {
  AppTokens get tokens => Theme.of(this).extension<AppTokens>()!;
}

/// 8-pt spacing scale.
abstract final class AppSpacing {
  static const double s4 = 4;
  static const double s8 = 8;
  static const double s16 = 16;
  static const double s24 = 24;
  static const double s32 = 32;
}
