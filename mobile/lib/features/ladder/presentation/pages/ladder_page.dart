import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import 'package:qala/app/router/routes.dart';
import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/core/l10n/l10n.dart';
import 'package:qala/features/ladder/presentation/cubit/ladder_cubit.dart';

class LadderPage extends StatelessWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final lang = Localizations.localeOf(context).languageCode;
    final entries = context.watch<LadderCubit>().state;
    return Scaffold(
      appBar: AppBar(title: Text(l10n.homeLadderTitle)),
      body: ListView.separated(
        padding: const EdgeInsets.all(AppSpacing.s16),
        itemCount: entries.length,
        separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.s8),
        itemBuilder: (context, i) {
          final e = entries[i];
          final scheme = Theme.of(context).colorScheme;
          return Card(
            child: ListTile(
              enabled: e.unlocked,
              leading: CircleAvatar(
                backgroundColor: e.beaten
                    ? context.tokens.success
                    : scheme.primary,
                foregroundColor: scheme.onPrimary,
                child: e.beaten
                    ? const Icon(Icons.check)
                    : e.unlocked
                    ? Text('${e.opponent.level}')
                    : const Icon(Icons.lock_outline, size: 18),
              ),
              title: Text(e.opponent.name(lang)),
              subtitle: Text(
                e.beaten
                    ? l10n.ladderBeaten
                    : e.unlocked
                    ? l10n.ladderLevel(e.opponent.level)
                    : l10n.ladderLocked,
              ),
              trailing: e.unlocked
                  ? Icon(Icons.play_arrow_rounded, color: scheme.primary)
                  : null,
              onTap: e.unlocked
                  ? () async {
                      await context.push(Routes.ladderGame(e.opponent.level));
                      if (context.mounted) {
                        context.read<LadderCubit>().refresh();
                      }
                    }
                  : null,
            ),
          );
        },
      ),
    );
  }
}
