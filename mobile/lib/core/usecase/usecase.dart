import 'package:fpdart/fpdart.dart';

import 'package:qala/core/error/failure.dart';

/// One business action. Returns a [Failure] instead of throwing.
abstract interface class UseCase<T, P> {
  Future<Either<Failure, T>> call(P params);
}

/// Parameter type for use cases that take none.
final class NoParams {
  const new();
}
