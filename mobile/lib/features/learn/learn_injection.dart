import 'package:flutter/services.dart';
import 'package:get_it/get_it.dart';

import 'package:qala/features/learn/data/datasources/lesson_asset_data_source.dart';
import 'package:qala/features/learn/data/repositories/lesson_repository_impl.dart';
import 'package:qala/features/learn/domain/repositories/lesson_repository.dart';
import 'package:qala/features/learn/domain/usecases/lesson_usecases.dart';
import 'package:qala/features/learn/presentation/cubit/learn_cubit.dart';

void registerLearn(GetIt getIt) {
  getIt
    ..registerLazySingleton(() => LessonAssetDataSource(rootBundle))
    ..registerLazySingleton<LessonRepository>(
      () => LessonRepositoryImpl(getIt(), getIt()),
    )
    ..registerFactory(() => GetChapters(getIt()))
    ..registerFactory(() => GetLessonStars(getIt()))
    ..registerFactory(() => CompleteLesson(getIt()))
    ..registerFactory(() => LearnCubit(getIt(), getIt()));
}
