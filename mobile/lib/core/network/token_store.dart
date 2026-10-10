import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Access/refresh tokens. Kept only in secure storage (Keychain/Keystore).
abstract interface class TokenStore {
  Future<String?> accessToken();
  Future<String?> refreshToken();
  Future<void> save({required String access, required String refresh});
  Future<void> clear();
}

class SecureTokenStore implements TokenStore {
  new(this._storage);

  final FlutterSecureStorage _storage;

  static const _access = 'auth.access';
  static const _refresh = 'auth.refresh';

  @override
  Future<String?> accessToken() => _storage.read(key: _access);

  @override
  Future<String?> refreshToken() => _storage.read(key: _refresh);

  @override
  Future<void> save({required String access, required String refresh}) async {
    await _storage.write(key: _access, value: access);
    await _storage.write(key: _refresh, value: refresh);
  }

  @override
  Future<void> clear() async {
    await _storage.delete(key: _access);
    await _storage.delete(key: _refresh);
  }
}
