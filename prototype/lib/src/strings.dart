import 'package:game_core/game_core.dart';

/// UI language. The prototype ships Arabic and English.
enum Lang { ar, en }

/// All UI text in one place, so playtests can run in either language.
class S {
  const S(this.lang);

  final Lang lang;

  bool get isAr => lang == Lang.ar;

  String _t(String en, String ar) => isAr ? ar : en;

  String get title => _t("Qal'a", 'قلعة');
  String get subtitle => _t(
    'Wells & Walls · playtest prototype',
    'الآبار والأسوار · نموذج للتجربة',
  );
  String get passAndPlay =>
      _t('2 players (pass & play)', 'لاعبان (على جهاز واحد)');
  String get vsAi => _t('Play against the AI', 'العب ضد الذكاء الاصطناعي');
  String get level => _t('Level', 'المستوى');
  String get youPlay => _t('You play', 'تلعب بـ');
  String get rules => _t('Rules', 'القواعد');
  String get start => _t('Start', 'ابدأ');
  String get howToPlay => _t('How to play', 'طريقة اللعب');
  String get language => _t('العربية', 'English');
  String get undo => _t('Undo', 'تراجع');
  String get newGame => _t('New game', 'لعبة جديدة');
  String get home => _t('Menu', 'القائمة');
  String get copyRecord => _t('Copy game record', 'انسخ سجل اللعبة');
  String get copied => _t('Game record copied', 'تم نسخ سجل اللعبة');
  String get thinking => _t('AI is thinking…', 'الذكاء الاصطناعي يفكر…');
  String get water => _t('Water', 'الماء');
  String get moveCounter => _t('Move', 'النقلة');
  String get noWater => _t(
    'No water: this piece is cut off. It can move, but it cannot capture.',
    'لا ماء: هذه القطعة مقطوعة عن الإمداد. يمكنها التحرك لكن لا يمكنها الأسر.',
  );
  String get cannotReach =>
      _t('That move is not allowed.', 'هذه النقلة غير مسموحة.');
  String get yourTurn => _t('Your turn', 'دورك');
  String get playAgain => _t('Play again', 'العب مجددًا');
  String get rulesV06 => _t('v0.6 (standard)', 'v0.6 (القياسية)');
  String get rulesV07 => _t('v0.7 (+1 water North)', 'v0.7 (+1 ماء للشمال)');

  String levelName(AiLevel level) => switch (level) {
    AiLevel.easy => _t('Easy', 'سهل'),
    AiLevel.medium => _t('Medium', 'متوسط'),
    AiLevel.hard => _t('Hard', 'صعب'),
  };

  String sideName(Side side) => switch (side) {
    Side.south => _t('South (sand)', 'الجنوب (الرملي)'),
    Side.north => _t('North (indigo)', 'الشمال (النيلي)'),
  };

  String turnOf(Side side) =>
      _t('${sideName(side)} to move', 'دور ${sideName(side)}');

  String pieceName(PieceType type) => isAr ? type.arabicName : type.englishName;

  /// Single character drawn on the piece.
  String pieceGlyph(PieceType type) => switch (type) {
    PieceType.amir => _t('A', 'أ'),
    PieceType.jundi => _t('J', 'ج'),
    PieceType.faris => _t('F', 'ف'),
    PieceType.rami => _t('R', 'ر'),
  };

  String winner(Outcome outcome) => outcome.isDraw
      ? _t('Draw', 'تعادل')
      : _t(
          '${sideName(outcome.winner!)} wins!',
          'فاز ${sideName(outcome.winner!)}!',
        );

  String reason(EndReason reason) => switch (reason) {
    EndReason.amirCaptured => _t('The Amir was captured.', 'تم أسر الأمير.'),
    EndReason.qalaTaken => _t(
      'A supplied piece took the enemy Qal\'a.',
      'قطعة متصلة بالإمداد احتلت قلعة الخصم.',
    ),
    EndReason.noLegalMoves => _t(
      'The opponent had no legal move.',
      'لم يبقَ للخصم أي نقلة.',
    ),
    EndReason.waterVictory => _t(
      'Reached 10 water points.',
      'وصل إلى 10 نقاط ماء.',
    ),
    EndReason.plyLimitWater => _t(
      'Move limit: more water.',
      'انتهت النقلات: ماء أكثر.',
    ),
    EndReason.plyLimitWells => _t(
      'Move limit: more Wells held.',
      'انتهت النقلات: آبار أكثر.',
    ),
    EndReason.plyLimitMaterial => _t(
      'Move limit: more pieces.',
      'انتهت النقلات: قطع أكثر.',
    ),
    EndReason.plyLimitDraw => _t(
      'Move limit with everything equal.',
      'انتهت النقلات بالتساوي.',
    ),
  };

  /// Short rules summary for the "How to play" sheet.
  List<(String, String)> get rulesSummary => isAr
      ? const [
          (
            'الهدف',
            'أسر أمير الخصم، أو احتلال قلعته بقطعة متصلة بالإمداد، أو جمع 10 نقاط ماء.',
          ),
          ('الدور', 'حرّك قطعة واحدة في كل دور. يبدأ الجنوب.'),
          ('الأمير (أ)', 'خطوة واحدة في أي اتجاه.'),
          ('الجندي (ج)', 'خطوة واحدة للأمام أو الخلف أو الجانب (ليس قطريًا).'),
          ('الفارس (ف)', 'ينزلق حتى 3 مربعات في خط مستقيم.'),
          (
            'الرامي (ر)',
            'يخطو قطريًا خطوة واحدة، ويرمي عدوًا على بُعد مربعين في خط مستقيم إذا كان المربع بينهما فارغًا. لا يتحرك عند الرمي.',
          ),
          (
            'الإمداد',
            'لا تأسر القطعة إلا إذا كانت متصلة (بالتلامس) بقلعتك أو بأميرك عبر سلسلة من قطعك. القطع المتصلة تتوهج، والمقطوعة باهتة.',
          ),
          (
            'الآبار',
            'في بداية دورك تكسب نقطة ماء عن كل بئر تحتله قطعة متصلة (ليس الأمير). 10 نقاط = فوز.',
          ),
          ('الحد', 'بعد 60 نقلة يفوز صاحب الماء الأكثر.'),
        ]
      : const [
          (
            'Goal',
            'Capture the enemy Amir, take the enemy Qal\'a with a supplied piece, or collect 10 water points.',
          ),
          ('Turn', 'Move one piece per turn. South moves first.'),
          ('Amir (A)', '1 step in any direction.'),
          ('Jundi (J)', '1 step forward, back or sideways (not diagonal).'),
          ('Faris (F)', 'Slides up to 3 squares in a straight line.'),
          (
            'Rami (R)',
            'Steps 1 square diagonally. Shoots an enemy 2 squares away in a straight line if the square between is empty; it stays put.',
          ),
          (
            'Supply',
            'A piece can capture only if it is linked (touching) to your Qal\'a or Amir through a chain of your pieces. Supplied pieces glow; cut-off pieces are faded.',
          ),
          (
            'Wells',
            'At the start of your turn: +1 water for each Well held by a supplied piece (not the Amir). 10 water wins.',
          ),
          ('Limit', 'After 60 moves, more water wins.'),
        ];
}

/// AI strength. Search depth and move noise.
enum AiLevel {
  easy(depth: 1, noise: 1.5),
  medium(depth: 2, noise: 0.3),
  hard(depth: 3, noise: 0.05);

  const AiLevel({required this.depth, required this.noise});

  final int depth;
  final double noise;
}
