import 'dart:async';

import 'package:flame/game.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:game_core/game_core.dart';

import '../board/board_game.dart';
import '../board/palette.dart';
import '../match_controller.dart';
import '../strings.dart';
import 'rules_sheet.dart';

/// One match: the board between two player panels, plus the controls a
/// playtest needs (undo, restart, copy the game record).
class GameScreen extends StatefulWidget {
  const GameScreen({
    super.key,
    required this.config,
    required this.strings,
    this.controller,
  });

  final MatchConfig config;
  final S strings;

  /// Injected in tests; otherwise created here.
  final MatchController? controller;

  @override
  State<GameScreen> createState() => _GameScreenState();
}

class _GameScreenState extends State<GameScreen> {
  late final MatchController _controller;
  late final BoardGame _game;
  late final StreamSubscription<Hint> _hints;
  bool _endShown = false;

  S get s => widget.strings;

  /// The human plays North against the AI: draw North at the bottom.
  bool get _flipped =>
      widget.config.vsAi && widget.config.humanSide == Side.north;

  @override
  void initState() {
    super.initState();
    _controller = widget.controller ?? MatchController(widget.config);
    _game = BoardGame(controller: _controller, strings: s, flipped: _flipped);
    _controller.addListener(_onChange);
    _hints = _controller.hints.listen(_showHint);
  }

  @override
  void dispose() {
    _hints.cancel();
    _controller.removeListener(_onChange);
    if (widget.controller == null) _controller.dispose();
    super.dispose();
  }

  void _onChange() {
    setState(() {});
    final outcome = _controller.state.outcome;
    if (outcome == null) {
      _endShown = false;
    } else if (!_endShown) {
      _endShown = true;
      // Let the last move animate before the dialog appears.
      Future<void>.delayed(const Duration(milliseconds: 600), () {
        if (mounted && _controller.state.isOver) _showEnd(outcome);
      });
    }
  }

  void _showHint(Hint hint) {
    final messenger = ScaffoldMessenger.of(context)..hideCurrentSnackBar();
    messenger.showSnackBar(
      SnackBar(
        content: Text(hint == Hint.noWater ? s.noWater : s.cannotReach),
        duration: const Duration(seconds: 3),
      ),
    );
  }

  Future<void> _copyRecord() async {
    await Clipboard.setData(
      ClipboardData(text: _controller.record().toString()),
    );
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(s.copied)));
  }

  Future<void> _showEnd(Outcome outcome) {
    return showDialog<void>(
      context: context,
      builder: (context) => Directionality(
        textDirection: s.isAr ? TextDirection.rtl : TextDirection.ltr,
        child: AlertDialog(
          title: Text(s.winner(outcome)),
          content: Text(s.reason(outcome.reason)),
          actions: [
            TextButton(onPressed: _copyRecord, child: Text(s.copyRecord)),
            TextButton(
              onPressed: () {
                Navigator.of(context)
                  ..pop()
                  ..pop();
              },
              child: Text(s.home),
            ),
            FilledButton(
              onPressed: () {
                Navigator.of(context).pop();
                _controller.restart();
              },
              child: Text(s.playAgain),
            ),
          ],
        ),
      ),
    );
  }

  String _status() {
    final state = _controller.state;
    if (state.isOver) return s.winner(state.outcome!);
    if (_controller.aiThinking) return s.thinking;
    if (widget.config.vsAi && state.toMove == widget.config.humanSide) {
      return s.yourTurn;
    }
    return s.turnOf(state.toMove);
  }

  @override
  Widget build(BuildContext context) {
    final state = _controller.state;
    final bottomSide = _flipped ? Side.north : Side.south;
    return Directionality(
      textDirection: s.isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        appBar: AppBar(
          title: Text(s.title),
          backgroundColor: Palette.background,
          actions: [
            IconButton(
              tooltip: s.howToPlay,
              icon: const Icon(Icons.help_outline),
              onPressed: () => showRulesSheet(context, s),
            ),
          ],
        ),
        backgroundColor: Palette.background,
        body: SafeArea(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: Column(
                children: [
                  _PlayerPanel(
                    side: bottomSide.opponent,
                    state: state,
                    strings: s,
                    isAi: widget.config.aiSide == bottomSide.opponent,
                  ),
                  Expanded(
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 8),
                      // The board keeps files a–g left to right in both
                      // languages.
                      child: Directionality(
                        textDirection: TextDirection.ltr,
                        child: GameWidget(key: const Key('board'), game: _game),
                      ),
                    ),
                  ),
                  _PlayerPanel(
                    side: bottomSide,
                    state: state,
                    strings: s,
                    isAi: widget.config.aiSide == bottomSide,
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 6),
                    child: Text(
                      '${_status()}   ·   ${s.moveCounter} '
                      '${state.ply}/${state.rules.plyLimit}',
                      key: const Key('status'),
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(8, 0, 8, 8),
                    child: Wrap(
                      alignment: WrapAlignment.center,
                      spacing: 8,
                      children: [
                        OutlinedButton.icon(
                          key: const Key('undo'),
                          onPressed: _controller.canUndo
                              ? _controller.undo
                              : null,
                          icon: const Icon(Icons.undo),
                          label: Text(s.undo),
                        ),
                        OutlinedButton.icon(
                          onPressed: _controller.restart,
                          icon: const Icon(Icons.refresh),
                          label: Text(s.newGame),
                        ),
                        OutlinedButton.icon(
                          key: const Key('copy'),
                          onPressed: _copyRecord,
                          icon: const Icon(Icons.copy_all_outlined),
                          label: Text(s.copyRecord),
                        ),
                      ],
                    ),
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

/// Name, water bar and piece count for one side. Highlighted on its turn.
class _PlayerPanel extends StatelessWidget {
  const _PlayerPanel({
    required this.side,
    required this.state,
    required this.strings,
    required this.isAi,
  });

  final Side side;
  final GameState state;
  final S strings;
  final bool isAi;

  @override
  Widget build(BuildContext context) {
    final active = !state.isOver && state.toMove == side;
    final target = state.rules.waterToWin ?? 10;
    final water = state.water(side);
    return AnimatedContainer(
      duration: const Duration(milliseconds: 200),
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: active ? Palette.lastMove : Colors.transparent,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: active ? Palette.selection : Palette.coordinates,
          width: active ? 2 : 1,
        ),
      ),
      child: Row(
        children: [
          CircleAvatar(
            radius: 10,
            backgroundColor: Palette.pieceFill(side),
            child: CircleAvatar(
              radius: 10,
              backgroundColor: Colors.transparent,
              foregroundColor: Palette.pieceEdge(side),
              child: Icon(isAi ? Icons.smart_toy : Icons.person, size: 13),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              strings.sideName(side),
              style: const TextStyle(fontWeight: FontWeight.w600),
              overflow: TextOverflow.ellipsis,
            ),
          ),
          const Icon(Icons.water_drop, size: 16, color: Palette.water),
          const SizedBox(width: 4),
          SizedBox(
            width: 90,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(
                key: Key('water-${side.name}'),
                value: (water / target).clamp(0, 1),
                minHeight: 8,
                color: Palette.water,
                backgroundColor: Palette.waterLight,
              ),
            ),
          ),
          const SizedBox(width: 6),
          Text('$water/$target'),
        ],
      ),
    );
  }
}
