import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:game_core/game_core.dart';
import 'package:go_router/go_router.dart';

import 'package:qala/app/di/injection.dart';
import 'package:qala/app/router/routes.dart';
import 'package:qala/app/theme/app_tokens.dart';
import 'package:qala/core/l10n/l10n.dart';
import 'package:qala/core/widgets/board/board_view_model.dart';
import 'package:qala/core/widgets/board/qala_board.dart';
import 'package:qala/features/game/domain/entities/game_mode.dart';
import 'package:qala/features/game/presentation/bloc/game_bloc.dart';
import 'package:qala/features/game/presentation/widgets/piece_glyph.dart';
import 'package:qala/features/game/presentation/widgets/player_panel.dart';

class GamePage extends StatelessWidget {
  const new({required this.mode, super.key});

  final GameMode mode;

  @override
  Widget build(BuildContext context) => BlocProvider(
    create: (_) => GameBloc(
      mode: mode,
      chooseAiMove: getIt(),
      saveGameResult: getIt(),
      recordLadderWin: getIt(),
    )..add(const GameStarted()),
    child: const GameView(),
  );
}

class GameView extends StatelessWidget {
  const new({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    final lang = Localizations.localeOf(context).languageCode;
    return BlocConsumer<GameBloc, PlayState>(
      listenWhen: (a, b) => a.effectSeq != b.effectSeq,
      listener: (context, state) => _onEffect(context, state.effect),
      builder: (context, state) {
        final mode = state.mode;
        final ladder = mode is LadderGame ? mode : null;
        final bottom = ladder?.humanSide ?? Side.south;
        String name(Side side) => ladder != null && side == ladder.aiSide
            ? ladder.opponent.name(lang)
            : side == Side.south
            ? l10n.sideSouth
            : l10n.sideNorth;
        final status = state.aiThinking
            ? l10n.aiThinking(ladder!.opponent.name(lang))
            : ladder != null && state.isHumanTurn
            ? l10n.yourTurn
            : l10n.turnOf(name(state.position.toMove));
        final counter = l10n.moveCounter(
          state.position.ply,
          state.position.rules.plyLimit,
        );
        return Scaffold(
          appBar: AppBar(
            title: Text(ladder?.opponent.name(lang) ?? l10n.homePassPlayTitle),
          ),
          body: SafeArea(
            child: Column(
              children: [
                PlayerPanel(
                  side: bottom.opponent,
                  position: state.position,
                  name: name(bottom.opponent),
                  isAi: ladder?.aiSide == bottom.opponent,
                ),
                Expanded(
                  child: Center(
                    child: Padding(
                      padding: const EdgeInsets.all(AppSpacing.s8),
                      child: QalaBoard(
                        model: BoardViewModel(
                          position: state.position,
                          selected: state.selected,
                          targets: state.targets,
                          lastMove: state.lastMove,
                          flipped: bottom == Side.north,
                        ),
                        glyph: (t) => pieceGlyph(context, t),
                        onSquareTap: (s) =>
                            context.read<GameBloc>().add(SquareTapped(s)),
                      ),
                    ),
                  ),
                ),
                PlayerPanel(
                  side: bottom,
                  position: state.position,
                  name: name(bottom),
                ),
                Padding(
                  padding: const EdgeInsets.all(AppSpacing.s8),
                  child: Text(
                    '$status  ·  $counter',
                    key: const Key('game-status'),
                  ),
                ),
                Padding(
                  padding: const EdgeInsetsDirectional.only(
                    bottom: AppSpacing.s8,
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      OutlinedButton.icon(
                        key: const Key('undo'),
                        onPressed: state.canUndo
                            ? () => context.read<GameBloc>().add(
                                const UndoPressed(),
                              )
                            : null,
                        icon: const Icon(Icons.undo),
                        label: Text(l10n.undo),
                      ),
                      const SizedBox(width: AppSpacing.s8),
                      OutlinedButton.icon(
                        onPressed: () => context.read<GameBloc>().add(
                          const RestartPressed(),
                        ),
                        icon: const Icon(Icons.refresh),
                        label: Text(l10n.restart),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _onEffect(BuildContext context, GameEffect? effect) {
    final l10n = context.l10n;
    switch (effect) {
      case NoWaterHint():
        _snack(context, l10n.hintNoWater);
      case NotAllowedHint():
        _snack(context, l10n.hintNotAllowed);
      case GameFinished(:final outcome, :final unlockedNext):
        unawaited(_showResult(context, outcome, unlockedNext: unlockedNext));
      case null:
        break;
    }
  }

  void _snack(BuildContext context, String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _showResult(
    BuildContext context,
    Outcome outcome, {
    required bool unlockedNext,
  }) async {
    final l10n = context.l10n;
    final bloc = context.read<GameBloc>();
    final mode = bloc.state.mode;
    final lang = Localizations.localeOf(context).languageCode;
    final String title;
    if (outcome.isDraw) {
      title = l10n.resultDraw;
    } else if (mode is LadderGame) {
      title = outcome.winner == mode.humanSide
          ? l10n.resultYouWin
          : l10n.resultYouLose(mode.opponent.name(lang));
    } else {
      title = l10n.resultWin(
        outcome.winner == Side.south ? l10n.sideSouth : l10n.sideNorth,
      );
    }
    final reason = switch (outcome.reason) {
      EndReason.amirCaptured => l10n.reasonAmirCaptured,
      EndReason.qalaTaken => l10n.reasonQalaTaken,
      EndReason.noLegalMoves => l10n.reasonNoLegalMoves,
      EndReason.waterVictory => l10n.reasonWaterVictory,
      _ => l10n.reasonPlyLimit,
    };
    await Future<void>.delayed(const Duration(milliseconds: 500));
    if (!context.mounted) return;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(title),
        content: Text(reason),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.of(dialogContext).pop();
              context.go(Routes.home);
            },
            child: Text(l10n.backHome),
          ),
          if (unlockedNext && mode is LadderGame)
            TextButton(
              onPressed: () {
                Navigator.of(dialogContext).pop();
                context.pushReplacement(
                  Routes.ladderGame(mode.opponent.level + 1),
                );
              },
              child: Text(l10n.nextOpponent),
            ),
          FilledButton(
            onPressed: () {
              Navigator.of(dialogContext).pop();
              bloc.add(const RestartPressed());
            },
            child: Text(l10n.playAgain),
          ),
        ],
      ),
    );
  }
}
