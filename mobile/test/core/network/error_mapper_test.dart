import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:qala/core/error/failure.dart';
import 'package:qala/core/network/error_mapper.dart';

DioException _response(int status, [Object? data]) {
  final options = RequestOptions(path: '/x');
  return DioException(
    requestOptions: options,
    type: DioExceptionType.badResponse,
    response: Response(requestOptions: options, statusCode: status, data: data),
  );
}

void main() {
  test('timeouts and connection errors are network failures', () {
    for (final type in [
      DioExceptionType.connectionTimeout,
      DioExceptionType.receiveTimeout,
      DioExceptionType.connectionError,
    ]) {
      final e = DioException(requestOptions: RequestOptions(), type: type);
      expect(ErrorMapper.fromDio(e), isA<NetworkFailure>());
    }
  });

  test('400 carries the backend messages', () {
    final failure = ErrorMapper.fromDio(
      _response(400, {
        'error': {
          'code': '400',
          'messages': ['Name is required', 'Too long'],
          'source': 'Validation',
        },
      }),
    );
    expect(failure, const ValidationFailure(['Name is required', 'Too long']));
  });

  test('status codes map to typed failures', () {
    expect(ErrorMapper.fromDio(_response(401)), isA<UnauthorizedFailure>());
    expect(ErrorMapper.fromDio(_response(403)), isA<ForbiddenFailure>());
    expect(ErrorMapper.fromDio(_response(404)), isA<NotFoundFailure>());
    expect(ErrorMapper.fromDio(_response(503)), isA<ServerFailure>());
  });
}
