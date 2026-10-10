import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/app/theme/brand_tokens.g.dart';

/// Material 3 light and dark themes built from the brand tokens.
abstract final class AppTheme {
  static ThemeData light(Locale locale) => _build(
    Brightness.light,
    locale,
    ColorScheme.fromSeed(seedColor: BrandLightScheme.primary).copyWith(
      primary: BrandLightScheme.primary,
      onPrimary: BrandLightScheme.onPrimary,
      secondary: BrandLightScheme.accent,
      onSecondary: BrandLightScheme.onAccent,
      surface: BrandLightScheme.surface,
      onSurface: BrandLightScheme.textPrimary,
      onSurfaceVariant: BrandLightScheme.textSecondary,
      outlineVariant: BrandLightScheme.borderSubtle,
      error: BrandPalette.danger500,
    ),
    AppTokens.light,
  );

  static ThemeData dark(Locale locale) => _build(
    Brightness.dark,
    locale,
    ColorScheme.fromSeed(
      seedColor: BrandLightScheme.primary,
      brightness: Brightness.dark,
    ).copyWith(
      primary: BrandDarkScheme.primary,
      onPrimary: BrandDarkScheme.onPrimary,
      secondary: BrandDarkScheme.accent,
      onSecondary: BrandDarkScheme.onAccent,
      surface: BrandDarkScheme.surface,
      onSurface: BrandDarkScheme.textPrimary,
      onSurfaceVariant: BrandDarkScheme.textSecondary,
      outlineVariant: BrandDarkScheme.borderSubtle,
      error: BrandPalette.danger500,
    ),
    AppTokens.dark,
  );

  static ThemeData _build(
    Brightness brightness,
    Locale locale,
    ColorScheme scheme,
    AppTokens tokens,
  ) {
    final font = locale.languageCode == 'ar'
        ? BrandFonts.arabic
        : BrandFonts.latin;
    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      fontFamily: font,
      fontFamilyFallback: const [BrandFonts.arabic, BrandFonts.latin],
      scaffoldBackgroundColor: tokens.surfaceSunken,
      extensions: [tokens],
      appBarTheme: AppBarTheme(
        backgroundColor: tokens.surfaceSunken,
        foregroundColor: scheme.onSurface,
        elevation: 0,
        scrolledUnderElevation: 1,
        centerTitle: false,
      ),
      cardTheme: CardThemeData(
        color: scheme.surface,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(BrandRadius.lg),
          side: BorderSide(color: scheme.outlineVariant),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(48, 48),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(BrandRadius.md),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(48, 48),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(BrandRadius.md),
          ),
        ),
      ),
      pageTransitionsTheme: const PageTransitionsTheme(
        builders: {
          TargetPlatform.android: FadeForwardsPageTransitionsBuilder(),
          TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
        },
      ),
    );
  }
}
