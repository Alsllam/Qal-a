import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

/// Builds one [Dio] per backend host. Interceptors are added by DI.
abstract final class DioFactory {
  static Dio create(
    String baseUrl, {
    List<Interceptor> interceptors = const [],
  }) {
    final dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 20),
        contentType: Headers.jsonContentType,
      ),
    );
    dio.interceptors.addAll(interceptors);
    if (kDebugMode) {
      // Never log headers (tokens) or bodies (personal data).
      dio.interceptors.add(
        LogInterceptor(requestHeader: false, responseHeader: false),
      );
    }
    return dio;
  }
}
