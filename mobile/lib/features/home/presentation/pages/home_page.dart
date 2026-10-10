import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';

import 'package:qala/app/di/injection.dart';
import 'package:qala/app/env/env.dart';
import 'package:qala/app/router/routes.dart';
import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/core/l10n/l10n.dart';

class HomePage extends StatelessWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final theme = Theme.of(context);
    final online = getIt<Env>().onlineEnabled;
    return Scaffold(
      appBar: AppBar(
        actions: [
          IconButton(
            tooltip: l10n.settingsTitle,
            icon: const Icon(Icons.settings_outlined),
            onPressed: () => context.push(Routes.settings),
          ),
        ],
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.s16),
          children: [
            Hero(
              tag: 'logo',
              child: SvgPicture.asset(
                'assets/svg/logo_mark.svg',
                height: 72,
                semanticsLabel: l10n.appTitle,
              ),
            ),
            const SizedBox(height: AppSpacing.s8),
            Text(
              l10n.appTitle,
              textAlign: TextAlign.center,
              style: theme.textTheme.displaySmall?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
            Text(
              l10n.appTagline,
              textAlign: TextAlign.center,
              style: theme.textTheme.titleMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: AppSpacing.s24),
            _ModeCard(
              key: const Key('home-learn'),
              icon: Icons.menu_book_rounded,
              title: l10n.homeLearnTitle,
              subtitle: l10n.homeLearnSubtitle,
              onTap: () => context.push(Routes.learn),
            ),
            _ModeCard(
              key: const Key('home-ladder'),
              icon: Icons.emoji_events_outlined,
              title: l10n.homeLadderTitle,
              subtitle: l10n.homeLadderSubtitle,
              onTap: () => context.push(Routes.ladder),
            ),
            _ModeCard(
              key: const Key('home-pvp'),
              icon: Icons.people_alt_outlined,
              title: l10n.homePassPlayTitle,
              subtitle: l10n.homePassPlaySubtitle,
              onTap: () => context.push(Routes.passAndPlay),
            ),
            _ModeCard(
              icon: Icons.public,
              title: l10n.homeOnlineTitle,
              subtitle: l10n.homeOnlineSubtitle,
              onTap: online ? () {} : null,
            ),
          ],
        ),
      ),
    );
  }
}

class _ModeCard extends StatefulWidget {
  const new({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
    super.key,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback? onTap;

  @override
  State<_ModeCard> createState() => _ModeCardState();
}

class _ModeCardState extends State<_ModeCard> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final enabled = widget.onTap != null;
    return Padding(
      padding: const EdgeInsetsDirectional.only(bottom: AppSpacing.s8),
      child: AnimatedScale(
        scale: _pressed ? 0.97 : 1,
        duration: const Duration(milliseconds: 120),
        child: Card(
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            onTap: widget.onTap,
            onHighlightChanged: (v) => setState(() => _pressed = v),
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.s16),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 24,
                    backgroundColor: enabled
                        ? scheme.primary
                        : scheme.outlineVariant,
                    foregroundColor: scheme.onPrimary,
                    child: Icon(widget.icon),
                  ),
                  const SizedBox(width: AppSpacing.s16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.title,
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                        Text(
                          widget.subtitle,
                          style: TextStyle(color: scheme.onSurfaceVariant),
                        ),
                      ],
                    ),
                  ),
                  if (enabled)
                    Icon(
                      Directionality.of(context) == TextDirection.rtl
                          ? Icons.chevron_left
                          : Icons.chevron_right,
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
