import 'package:qala/features/settings/domain/entities/app_settings.dart';
import 'package:qala/features/settings/domain/repositories/settings_repository.dart';

class LoadSettings {
  const new(this._repository);

  final SettingsRepository _repository;

  AppSettings call() => _repository.load();
}

class SaveSettings {
  const new(this._repository);

  final SettingsRepository _repository;

  Future<void> call(AppSettings settings) => _repository.save(settings);
}
