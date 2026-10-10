import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/core/l10n/l10n.dart';
import 'package:qala/features/settings/domain/entities/app_settings.dart';
import 'package:qala/features/settings/presentation/cubit/settings_cubit.dart';

class SettingsPage extends StatelessWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final settings = context.watch<SettingsCubit>().state;
    final cubit = context.read<SettingsCubit>();
    final language =
        settings.languageCode ?? Localizations.localeOf(context).languageCode;
    return Scaffold(
      appBar: AppBar(title: Text(l10n.settingsTitle)),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.s16),
        children: [
          Text(
            l10n.settingsLanguage,
            style: Theme.of(context).textTheme.titleSmall,
          ),
          const SizedBox(height: AppSpacing.s8),
          SegmentedButton<String>(
            segments: const [
              ButtonSegment(value: 'ar', label: Text('العربية')),
              ButtonSegment(value: 'en', label: Text('English')),
            ],
            selected: {language},
            onSelectionChanged: (v) => cubit.setLanguage(v.first),
          ),
          const SizedBox(height: AppSpacing.s24),
          Text(
            l10n.settingsTheme,
            style: Theme.of(context).textTheme.titleSmall,
          ),
          const SizedBox(height: AppSpacing.s8),
          SegmentedButton<AppThemeMode>(
            segments: [
              ButtonSegment(
                value: AppThemeMode.system,
                label: Text(l10n.settingsThemeSystem),
              ),
              ButtonSegment(
                value: AppThemeMode.light,
                label: Text(l10n.settingsThemeLight),
              ),
              ButtonSegment(
                value: AppThemeMode.dark,
                label: Text(l10n.settingsThemeDark),
              ),
            ],
            selected: {settings.themeMode},
            onSelectionChanged: (v) => cubit.setThemeMode(v.first),
          ),
          const SizedBox(height: AppSpacing.s16),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(l10n.settingsCoachTips),
            value: settings.coachTips,
            onChanged: (v) => cubit.setCoachTips(enabled: v),
          ),
        ],
      ),
    );
  }
}
