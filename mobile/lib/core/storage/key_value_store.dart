import 'package:shared_preferences/shared_preferences.dart';

/// Non-sensitive settings and progress. Tokens never go here.
abstract interface class KeyValueStore {
  String? getString(String key);
  int? getInt(String key);
  Future<void> setString(String key, String value);
  Future<void> setInt(String key, int value);
  Future<void> remove(String key);
}

class PrefsKeyValueStore implements KeyValueStore {
  new(this._prefs);

  final SharedPreferences _prefs;

  @override
  String? getString(String key) => _prefs.getString(key);

  @override
  int? getInt(String key) => _prefs.getInt(key);

  @override
  Future<void> setString(String key, String value) =>
      _prefs.setString(key, value);

  @override
  Future<void> setInt(String key, int value) => _prefs.setInt(key, value);

  @override
  Future<void> remove(String key) => _prefs.remove(key);
}

/// In-memory store for tests.
class MemoryKeyValueStore implements KeyValueStore {
  final Map<String, Object> _values = {};

  @override
  String? getString(String key) => _values[key] as String?;

  @override
  int? getInt(String key) => _values[key] as int?;

  @override
  Future<void> setString(String key, String value) async =>
      _values[key] = value;

  @override
  Future<void> setInt(String key, int value) async => _values[key] = value;

  @override
  Future<void> remove(String key) async => _values.remove(key);
}
