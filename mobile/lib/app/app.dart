import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:go_router/go_router.dart';
import 'package:qala/app/di/injection.dart';
import 'package:qala/app/router/app_router.dart';
import 'package:qala/app/theme/app_theme.dart';
import 'package:qala/core/l10n/l10n.dart';
import 'package:qala/features/settings/domain/entities/app_settings.dart';
import 'package:qala/features/settings/presentation/cubit/settings_cubit.dart';

class App extends StatefulWidget {
  const new({super.key, this.router});

  /// Injected in tests.
  final GoRouter? router;

  @override
  State<App> createState() => _AppState();
}

class _AppState extends State<App> {
  late final GoRouter _router = widget.router ?? createRouter();

  @override
  Widget build(BuildContext context) {
    return BlocProvider.value(
      value: getIt<SettingsCubit>(),
      child: BlocBuilder<SettingsCubit, AppSettings>(
        builder: (context, settings) {
          final code = settings.languageCode;
          final locale = code == null ? null : Locale(code);
          final themeLocale =
              locale ?? WidgetsBinding.instance.platformDispatcher.locale;
          return MaterialApp.router(
            routerConfig: _router,
            onGenerateTitle: (c) => c.l10n.appTitle,
            debugShowCheckedModeBanner: false,
            locale: locale,
            supportedLocales: AppLocalizations.supportedLocales,
            localizationsDelegates: const [
              AppLocalizations.delegate,
              GlobalMaterialLocalizations.delegate,
              GlobalWidgetsLocalizations.delegate,
              GlobalCupertinoLocalizations.delegate,
            ],
            theme: AppTheme.light(themeLocale),
            darkTheme: AppTheme.dark(themeLocale),
            themeMode: switch (settings.themeMode) {
              AppThemeMode.system => ThemeMode.system,
              AppThemeMode.light => ThemeMode.light,
              AppThemeMode.dark => ThemeMode.dark,
            },
          );
        },
      ),
    );
  }
}
