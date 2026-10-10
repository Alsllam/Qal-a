import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:qala/app/env/env.dart';
import 'package:qala/core/network/token_store.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/features/game/game_injection.dart';
import 'package:qala/features/ladder/ladder_injection.dart';
import 'package:qala/features/learn/learn_injection.dart';
import 'package:qala/features/settings/settings_injection.dart';
import 'package:sembast/sembast.dart';

final GetIt getIt = GetIt.instance;

/// Registers core services, then each feature.
Future<void> configureDependencies({
  required Env env,
  required KeyValueStore store,
  required Database database,
}) async {
  getIt
    ..registerSingleton<Env>(env)
    ..registerSingleton<KeyValueStore>(store)
    ..registerSingleton<Database>(database)
    ..registerLazySingleton<TokenStore>(
      () => SecureTokenStore(const FlutterSecureStorage()),
    );
  // Network clients (DioFactory + AuthInterceptor) are registered with the
  // online feature (M2), when there is a backend to talk to.
  registerSettings(getIt);
  registerGame(getIt);
  registerLadder(getIt);
  registerLearn(getIt);
}
