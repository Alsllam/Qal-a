import 'package:flutter_test/flutter_test.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/settings/data/repositories/settings_repository_impl.dart';
import 'package:qala/features/settings/domain/entities/app_settings.dart';
import 'package:qala/features/settings/domain/usecases/settings_usecases.dart';
import 'package:qala/features/settings/presentation/cubit/settings_cubit.dart';

void main() {
  test('defaults follow the device and keep coach tips on', () {
    final repo = SettingsRepositoryImpl(MemoryKeyValueStore());
    expect(repo.load(), const AppSettings());
  });

  test('the cubit saves every change', () async {
    final store = MemoryKeyValueStore();
    final repo = SettingsRepositoryImpl(store);
    final cubit = SettingsCubit(
      load: LoadSettings(repo),
      save: SaveSettings(repo),
    );
    await cubit.setLanguage('ar');
    await cubit.setThemeMode(AppThemeMode.dark);
    await cubit.setCoachTips(enabled: false);
    expect(
      repo.load(),
      const AppSettings(
        languageCode: 'ar',
        themeMode: AppThemeMode.dark,
        coachTips: false,
      ),
    );
    await cubit.close();
  });
}
