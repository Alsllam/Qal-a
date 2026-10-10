import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/settings/domain/entities/app_settings.dart';
import 'package:qala/features/settings/domain/repositories/settings_repository.dart';

class SettingsRepositoryImpl implements SettingsRepository {
  new(this._store);

  final KeyValueStore _store;

  static const _language = 'settings.language';
  static const _theme = 'settings.theme';
  static const _coachTips = 'settings.coachTips';

  @override
  AppSettings load() => AppSettings(
    languageCode: _store.getString(_language),
    themeMode: AppThemeMode.values.firstWhere(
      (m) => m.name == _store.getString(_theme),
      orElse: () => AppThemeMode.system,
    ),
    coachTips: (_store.getInt(_coachTips) ?? 1) == 1,
  );

  @override
  Future<void> save(AppSettings settings) async {
    final language = settings.languageCode;
    if (language == null) {
      await _store.remove(_language);
    } else {
      await _store.setString(_language, language);
    }
    await _store.setString(_theme, settings.themeMode.name);
    await _store.setInt(_coachTips, settings.coachTips ? 1 : 0);
  }
}
