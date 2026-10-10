import 'package:qala/features/settings/domain/entities/app_settings.dart';

abstract interface class SettingsRepository {
  AppSettings load();
  Future<void> save(AppSettings settings);
}
