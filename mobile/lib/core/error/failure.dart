import 'package:equatable/equatable.dart';

/// Typed failure returned by use cases (never thrown across layers).
sealed class Failure extends Equatable {
  const new(this.message);

  final String message;

  @override
  List<Object?> get props => [runtimeType, message];
}

/// No connection or timeout.
final class NetworkFailure extends Failure {
  const new([super.message = '']);
}

/// 401 after the token refresh failed.
final class UnauthorizedFailure extends Failure {
  const new([super.message = '']);
}

/// 403.
final class ForbiddenFailure extends Failure {
  const new([super.message = '']);
}

/// 404.
final class NotFoundFailure extends Failure {
  const new([super.message = '']);
}

/// 400 with the backend's (already localized) validation messages.
final class ValidationFailure extends Failure {
  const new(this.messages) : super('');

  final List<String> messages;

  @override
  String get message => messages.join('\n');

  @override
  List<Object?> get props => [runtimeType, messages];
}

/// 5xx.
final class ServerFailure extends Failure {
  const new([super.message = '']);
}

/// Local storage or asset problem.
final class CacheFailure extends Failure {
  const new([super.message = '']);
}
