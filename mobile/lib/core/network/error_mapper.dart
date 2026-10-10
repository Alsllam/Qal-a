import 'package:dio/dio.dart';

import 'package:qala/core/error/failure.dart';

/// Maps a [DioException] and the backend error body
/// `{ "error": { "code", "messages": [...], "source" } }` to a [Failure].
abstract final class ErrorMapper {
  static Failure fromDio(DioException e) {
    const offline = {
      DioExceptionType.connectionTimeout,
      DioExceptionType.sendTimeout,
      DioExceptionType.receiveTimeout,
      DioExceptionType.connectionError,
    };
    if (offline.contains(e.type)) return const NetworkFailure();
    final status = e.response?.statusCode;
    final messages = _messages(e.response?.data);
    final message = messages.join('\n');
    return switch (status) {
      400 => ValidationFailure(messages),
      401 => UnauthorizedFailure(message),
      403 => ForbiddenFailure(message),
      404 => NotFoundFailure(message),
      final int s when s >= 500 => ServerFailure(message),
      null => const NetworkFailure(),
      _ => ServerFailure(message),
    };
  }

  static List<String> _messages(Object? data) {
    if (data is Map && data['error'] is Map) {
      final raw = (data['error'] as Map)['messages'];
      if (raw is List) return raw.map((m) => '$m').toList();
    }
    return const [];
  }
}
