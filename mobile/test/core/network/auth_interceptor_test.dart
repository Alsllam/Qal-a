import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:qala/core/network/auth_interceptor.dart';
import 'package:qala/core/network/token_store.dart';

class MemoryTokens implements TokenStore {
  String? access = 'old';
  String? refresh = 'r1';
  int cleared = 0;

  @override
  Future<String?> accessToken() async => access;

  @override
  Future<String?> refreshToken() async => refresh;

  @override
  Future<void> save({required String access, required String refresh}) async {
    this.access = access;
    this.refresh = refresh;
  }

  @override
  Future<void> clear() async {
    cleared++;
    access = null;
    refresh = null;
  }
}

/// Answers 401 unless the request carries `Bearer new`.
class TokenCheckingAdapter implements HttpClientAdapter {
  final List<String?> seen = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    final auth = options.headers['Authorization'] as String?;
    seen.add(auth);
    final ok = auth == 'Bearer new';
    return ResponseBody.fromString(
      jsonEncode({'ok': ok}),
      ok ? 200 : 401,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  late MemoryTokens tokens;
  late TokenCheckingAdapter adapter;
  late Dio dio;
  late int refreshCalls;
  late int expired;

  Dio build({bool refreshFails = false}) {
    final retry = Dio()..httpClientAdapter = adapter;
    return Dio()
      ..httpClientAdapter = adapter
      ..interceptors.add(
        AuthInterceptor(
          tokens: tokens,
          retryClient: retry,
          onSessionExpired: () => expired++,
          refresh: (token) async {
            refreshCalls++;
            await Future<void>.delayed(const Duration(milliseconds: 10));
            if (refreshFails) throw Exception('refresh rejected');
            return (access: 'new', refresh: 'r2');
          },
        ),
      );
  }

  setUp(() {
    tokens = MemoryTokens();
    adapter = TokenCheckingAdapter();
    refreshCalls = 0;
    expired = 0;
    dio = build();
  });

  test('adds the stored token and retries once after refreshing', () async {
    final response = await dio.get<Map<String, dynamic>>('/me');
    expect(response.statusCode, 200);
    expect(adapter.seen, ['Bearer old', 'Bearer new']);
    expect(refreshCalls, 1);
    expect(tokens.access, 'new');
    expect(tokens.refresh, 'r2');
  });

  test('concurrent 401s share one refresh', () async {
    final results = await Future.wait([
      dio.get<Map<String, dynamic>>('/a'),
      dio.get<Map<String, dynamic>>('/b'),
      dio.get<Map<String, dynamic>>('/c'),
    ]);
    expect(results.every((r) => r.statusCode == 200), isTrue);
    expect(refreshCalls, 1);
  });

  test('a failed refresh clears the session and reports it', () async {
    dio = build(refreshFails: true);
    await expectLater(
      dio.get<Map<String, dynamic>>('/me'),
      throwsA(isA<DioException>()),
    );
    expect(tokens.cleared, 1);
    expect(expired, 1);
  });
}
