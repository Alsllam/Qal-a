import 'package:flutter/material.dart';
import 'package:game_core/game_core.dart';

import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/core/l10n/l10n.dart';

/// Name, water bar and turn highlight for one side.
class PlayerPanel extends StatelessWidget {
  const new({
    required this.side,
    required this.position,
    required this.name,
    this.isAi = false,
    super.key,
  });

  final Side side;
  final GameState position;
  final String name;
  final bool isAi;

  @override
  Widget build(BuildContext context) {
    final tokens = context.tokens;
    final scheme = Theme.of(context).colorScheme;
    final active = !position.isOver && position.toMove == side;
    final target = position.rules.waterToWin ?? 10;
    final water = position.water(side);
    return AnimatedContainer(
      duration: const Duration(milliseconds: 200),
      margin: const EdgeInsetsDirectional.symmetric(
        horizontal: AppSpacing.s16,
        vertical: AppSpacing.s4,
      ),
      padding: const EdgeInsetsDirectional.symmetric(
        horizontal: AppSpacing.s16,
        vertical: AppSpacing.s8,
      ),
      decoration: BoxDecoration(
        color: active
            ? tokens.selection.withValues(alpha: .15)
            : scheme.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: active ? tokens.selection : scheme.outlineVariant,
          width: active ? 2 : 1,
        ),
      ),
      child: Row(
        children: [
          CircleAvatar(
            radius: 12,
            backgroundColor: side == Side.south
                ? tokens.southFill
                : tokens.northFill,
            foregroundColor: side == Side.south
                ? tokens.southEdge
                : tokens.southFill,
            child: Icon(
              isAi ? Icons.smart_toy_outlined : Icons.person,
              size: 14,
            ),
          ),
          const SizedBox(width: AppSpacing.s8),
          Expanded(
            child: Text(
              name,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
          Icon(Icons.water_drop, size: 16, color: tokens.water),
          const SizedBox(width: AppSpacing.s4),
          SizedBox(
            width: 80,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(
                value: (water / target).clamp(0, 1),
                minHeight: 8,
                color: tokens.water,
                backgroundColor: tokens.waterLight,
                semanticsLabel: context.l10n.waterLabel(water, target),
              ),
            ),
          ),
          const SizedBox(width: AppSpacing.s8),
          Text('$water/$target'),
        ],
      ),
    );
  }
}
