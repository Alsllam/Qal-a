import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:qala/app/app.dart';
import 'package:qala/app/di/injection.dart';
import 'package:qala/app/env/env.dart';
import 'package:qala/core/storage/key_value_store.dart';
import 'package:qala/core/storage/sembast_db.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Ordered startup (flutter-clean-mobile §3). Firebase, Crashlytics, Remote
/// Config and push are added when the Firebase projects exist (see README).
Future<void> bootstrap(Flavor flavor) async {
  await runZonedGuarded(
    () async {
      WidgetsFlutterBinding.ensureInitialized();
      FlutterError.onError = (details) {
        FlutterError.presentError(details);
        // TODO(crashlytics): record non-fatal when Firebase is configured.
      };
      final env = Env.fromDefines(flavor);
      final prefs = await SharedPreferences.getInstance();
      final database = await SembastDb.open();
      await configureDependencies(
        env: env,
        store: PrefsKeyValueStore(prefs),
        database: database,
      );
      runApp(const App());
    },
    (error, stack) {
      if (kDebugMode) debugPrint('Uncaught: $error\n$stack');
    },
  );
}
