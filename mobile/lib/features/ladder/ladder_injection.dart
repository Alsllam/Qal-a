import 'package:get_it/get_it.dart';

import 'package:qala/features/ladder/data/repositories/ladder_repository_impl.dart';
import 'package:qala/features/ladder/domain/repositories/ladder_repository.dart';
import 'package:qala/features/ladder/domain/usecases/ladder_usecases.dart';
import 'package:qala/features/ladder/presentation/cubit/ladder_cubit.dart';

void registerLadder(GetIt getIt) {
  getIt
    ..registerLazySingleton<LadderRepository>(
      () => LadderRepositoryImpl(getIt()),
    )
    ..registerFactory(() => GetLadder(getIt()))
    ..registerFactory(() => RecordLadderWin(getIt()))
    ..registerFactory(() => LadderCubit(getIt()));
}
