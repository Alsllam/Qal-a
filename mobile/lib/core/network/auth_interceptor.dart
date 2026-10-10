import 'dart:async';

import 'package:dio/dio.dart';

import 'package:qala/core/network/token_store.dart';

/// Exchanges a refresh token for new tokens. Uses its own bare Dio.
typedef TokenRefresher = Future<({String access, String refresh})> Function(
  String refreshToken,
);

/// Adds the access token (read from secure storage on every request). On 401
/// it refreshes once; concurrent 401s wait for that same refresh. If the
/// refresh fails, the session is cleared and `onSessionExpired` is called
/// (the auth bloc logs out; the interceptor never navigates).
class AuthInterceptor extends QueuedInterceptor {
  new({
    required this._tokens,
    required this._refresh,
    required Dio retryClient,
    required this._onSessionExpired,
  }) : _retry = retryClient;

  final TokenStore _tokens;
  final TokenRefresher _refresh;
  final Dio _retry;
  final void Function() _onSessionExpired;
  Future<String?>? _inFlight;

  static const _retriedFlag = 'auth.retried';

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final token = await _tokens.accessToken();
    if (token != null) options.headers['Authorization'] = 'Bearer $token';
    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final options = err.requestOptions;
    if (err.response?.statusCode != 401 ||
        options.extra[_retriedFlag] == true) {
      handler.next(err);
      return;
    }
    // Errors are handled one at a time: if another request already
    // refreshed, the stored token differs from the one this request used.
    final used = (options.headers['Authorization'] as String?)?.replaceFirst(
      'Bearer ',
      '',
    );
    final current = await _tokens.accessToken();
    final String? newToken;
    if (current != null && current != used) {
      newToken = current;
    } else {
      newToken = await (_inFlight ??= _doRefresh());
      _inFlight = null;
    }
    if (newToken == null) {
      handler.next(err);
      return;
    }
    options
      ..headers['Authorization'] = 'Bearer $newToken'
      ..extra[_retriedFlag] = true;
    try {
      handler.resolve(await _retry.fetch<dynamic>(options));
    } on DioException catch (e) {
      handler.next(e);
    }
  }

  Future<String?> _doRefresh() async {
    final refreshToken = await _tokens.refreshToken();
    if (refreshToken == null) {
      await _expire();
      return null;
    }
    try {
      final result = await _refresh(refreshToken);
      await _tokens.save(access: result.access, refresh: result.refresh);
      return result.access;
    } on Object {
      await _expire();
      return null;
    }
  }

  Future<void> _expire() async {
    await _tokens.clear();
    _onSessionExpired();
  }
}
