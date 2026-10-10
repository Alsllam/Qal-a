import 'package:flutter_bloc/flutter_bloc.dart';

import 'package:qala/features/settings/domain/entities/app_settings.dart';
import 'package:qala/features/settings/domain/usecases/settings_usecases.dart';

/// App-wide settings (language, theme, coach tips). Changing the language
/// rebuilds MaterialApp; the app never restarts.
class SettingsCubit extends Cubit<AppSettings> {
  new({required LoadSettings load, required this._save}) : super(load());

  final SaveSettings _save;

  Future<void> setLanguage(String code) =>
      _update(state.copyWith(languageCode: code));

  Future<void> setThemeMode(AppThemeMode mode) =>
      _update(state.copyWith(themeMode: mode));

  Future<void> setCoachTips({required bool enabled}) =>
      _update(state.copyWith(coachTips: enabled));

  Future<void> _update(AppSettings next) async {
    emit(next);
    await _save(next);
  }
}
