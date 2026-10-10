import 'package:equatable/equatable.dart';

enum AppThemeMode { system, light, dark }

/// User preferences stored on the device.
class AppSettings extends Equatable {
  const new({
    this.languageCode,
    this.themeMode = AppThemeMode.system,
    this.coachTips = true,
  });

  /// Null means "follow the device language".
  final String? languageCode;
  final AppThemeMode themeMode;
  final bool coachTips;

  AppSettings copyWith({
    String? languageCode,
    AppThemeMode? themeMode,
    bool? coachTips,
  }) => AppSettings(
    languageCode: languageCode ?? this.languageCode,
    themeMode: themeMode ?? this.themeMode,
    coachTips: coachTips ?? this.coachTips,
  );

  @override
  List<Object?> get props => [languageCode, themeMode, coachTips];
}
