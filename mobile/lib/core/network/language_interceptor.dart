import 'package:dio/dio.dart';

/// Sends the UI language and the timezone offset with every request.
class LanguageInterceptor extends Interceptor {
  new(this._languageCode);

  final String Function() _languageCode;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    options.headers['Accept-Language'] = _languageCode();
    options.headers['X-Timezone-Offset'] =
        '${DateTime.now().timeZoneOffset.inMinutes}';
    handler.next(options);
  }
}
