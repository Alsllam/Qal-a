import 'package:get_it/get_it.dart';

import 'package:qala/features/settings/data/repositories/settings_repository_impl.dart';
import 'package:qala/features/settings/domain/repositories/settings_repository.dart';
import 'package:qala/features/settings/domain/usecases/settings_usecases.dart';
import 'package:qala/features/settings/presentation/cubit/settings_cubit.dart';

void registerSettings(GetIt getIt) {
  getIt
    ..registerLazySingleton<SettingsRepository>(
      () => SettingsRepositoryImpl(getIt()),
    )
    ..registerFactory(() => LoadSettings(getIt()))
    ..registerFactory(() => SaveSettings(getIt()))
    ..registerLazySingleton(() => SettingsCubit(load: getIt(), save: getIt()));
}
