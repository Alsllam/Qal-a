import 'package:get_it/get_it.dart';

import 'package:qala/features/game/data/datasources/game_history_local_data_source.dart';
import 'package:qala/features/game/data/repositories/game_history_repository_impl.dart';
import 'package:qala/features/game/data/services/isolate_ai_move_service.dart';
import 'package:qala/features/game/domain/repositories/game_history_repository.dart';
import 'package:qala/features/game/domain/services/ai_move_service.dart';
import 'package:qala/features/game/domain/usecases/game_usecases.dart';

void registerGame(GetIt getIt) {
  getIt
    ..registerLazySingleton<AiMoveService>(() => const IsolateAiMoveService())
    ..registerLazySingleton(() => GameHistoryLocalDataSource(getIt()))
    ..registerLazySingleton<GameHistoryRepository>(
      () => GameHistoryRepositoryImpl(getIt()),
    )
    ..registerFactory(() => ChooseAiMove(getIt()))
    ..registerFactory(() => SaveGameResult(getIt()));
}
