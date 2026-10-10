import 'package:flutter/material.dart';
import 'package:game_core/game_core.dart';

import '../match_controller.dart';
import '../strings.dart';
import 'game_screen.dart';
import 'rules_sheet.dart';

/// Version 0.7 of the rules: v0.6 plus 1 starting water point for North.
/// Kept selectable for playtests (see docs/balance-log.md).
const rulesV07 = RuleSet(
  version: '0.7',
  waterToWin: 10,
  wellsAreSources: false,
  waterNeedsSupply: true,
  amirEarnsWater: false,
  diagonalShots: false,
  northStartWater: 1,
);

/// Mode selection: pass-and-play or against the AI.
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  Lang _lang = Lang.ar;
  bool _vsAi = false;
  AiLevel _level = AiLevel.medium;
  Side _humanSide = Side.south;
  RuleSet _rules = RuleSet.standard;

  S get _s => S(_lang);

  void _start() {
    final config = MatchConfig(
      rules: _rules,
      aiLevel: _vsAi ? _level : null,
      humanSide: _humanSide,
    );
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => GameScreen(config: config, strings: _s),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final s = _s;
    final theme = Theme.of(context);
    return Directionality(
      textDirection: s.isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        body: SafeArea(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  Align(
                    alignment: AlignmentDirectional.centerEnd,
                    child: TextButton(
                      key: const Key('language'),
                      onPressed: () =>
                          setState(() => _lang = s.isAr ? Lang.en : Lang.ar),
                      child: Text(s.language),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    s.title,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.displayMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    s.subtitle,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.titleMedium,
                  ),
                  const SizedBox(height: 28),
                  _ModeCard(
                    key: const Key('mode-pvp'),
                    selected: !_vsAi,
                    icon: Icons.people_alt_outlined,
                    label: s.passAndPlay,
                    onTap: () => setState(() => _vsAi = false),
                  ),
                  const SizedBox(height: 12),
                  _ModeCard(
                    key: const Key('mode-ai'),
                    selected: _vsAi,
                    icon: Icons.smart_toy_outlined,
                    label: s.vsAi,
                    onTap: () => setState(() => _vsAi = true),
                  ),
                  if (_vsAi) ...[
                    const SizedBox(height: 16),
                    Text(s.level, style: theme.textTheme.labelLarge),
                    const SizedBox(height: 6),
                    SegmentedButton<AiLevel>(
                      segments: [
                        for (final l in AiLevel.values)
                          ButtonSegment(value: l, label: Text(s.levelName(l))),
                      ],
                      selected: {_level},
                      onSelectionChanged: (v) =>
                          setState(() => _level = v.first),
                    ),
                    const SizedBox(height: 12),
                    Text(s.youPlay, style: theme.textTheme.labelLarge),
                    const SizedBox(height: 6),
                    SegmentedButton<Side>(
                      segments: [
                        for (final side in Side.values)
                          ButtonSegment(
                            value: side,
                            label: Text(s.sideName(side)),
                          ),
                      ],
                      selected: {_humanSide},
                      onSelectionChanged: (v) =>
                          setState(() => _humanSide = v.first),
                    ),
                  ],
                  const SizedBox(height: 16),
                  Text(s.rules, style: theme.textTheme.labelLarge),
                  const SizedBox(height: 6),
                  SegmentedButton<String>(
                    segments: [
                      ButtonSegment(value: '0.6', label: Text(s.rulesV06)),
                      ButtonSegment(value: '0.7', label: Text(s.rulesV07)),
                    ],
                    selected: {_rules.version},
                    onSelectionChanged: (v) => setState(
                      () => _rules = v.first == '0.7'
                          ? rulesV07
                          : RuleSet.standard,
                    ),
                  ),
                  const SizedBox(height: 28),
                  FilledButton.icon(
                    key: const Key('start'),
                    onPressed: _start,
                    icon: const Icon(Icons.play_arrow),
                    label: Text(s.start),
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 16),
                    ),
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton.icon(
                    onPressed: () => showRulesSheet(context, s),
                    icon: const Icon(Icons.menu_book_outlined),
                    label: Text(s.howToPlay),
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

class _ModeCard extends StatelessWidget {
  const _ModeCard({
    super.key,
    required this.selected,
    required this.icon,
    required this.label,
    required this.onTap,
  });

  final bool selected;
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Material(
      color: selected
          ? scheme.primaryContainer
          : scheme.surfaceContainerHighest,
      borderRadius: BorderRadius.circular(14),
      child: InkWell(
        borderRadius: BorderRadius.circular(14),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Icon(icon, size: 28),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  label,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              if (selected) const Icon(Icons.check_circle),
            ],
          ),
        ),
      ),
    );
  }
}
