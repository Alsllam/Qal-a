// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'Qal\'a';

  @override
  String get appTagline => 'Fortresses, wells and water';

  @override
  String get homeLearnTitle => 'Learn';

  @override
  String get homeLearnSubtitle => 'Short lessons, one idea each';

  @override
  String get homeLadderTitle => 'AI ladder';

  @override
  String get homeLadderSubtitle => 'Beat eight opponents, one by one';

  @override
  String get homePassPlayTitle => 'Two players';

  @override
  String get homePassPlaySubtitle => 'Play a friend on one device';

  @override
  String get homeOnlineTitle => 'Online';

  @override
  String get homeOnlineSubtitle => 'Coming soon';

  @override
  String get settingsTitle => 'Settings';

  @override
  String get settingsLanguage => 'Language';

  @override
  String get settingsTheme => 'Theme';

  @override
  String get settingsThemeSystem => 'System';

  @override
  String get settingsThemeLight => 'Light';

  @override
  String get settingsThemeDark => 'Dark';

  @override
  String get settingsCoachTips => 'Coach tips during games';

  @override
  String get sideSouth => 'South';

  @override
  String get sideNorth => 'North';

  @override
  String turnOf(String side) {
    return '$side to move';
  }

  @override
  String get yourTurn => 'Your turn';

  @override
  String aiThinking(String name) {
    return '$name is thinking…';
  }

  @override
  String moveCounter(int ply, int limit) {
    return 'Move $ply/$limit';
  }

  @override
  String waterLabel(int value, int target) {
    return 'Water $value/$target';
  }

  @override
  String get undo => 'Undo';

  @override
  String get restart => 'New game';

  @override
  String get resign => 'Resign';

  @override
  String get hintNoWater =>
      'No water: this piece is cut off. It can move, but it cannot capture.';

  @override
  String get hintNotAllowed => 'That move is not allowed.';

  @override
  String resultWin(String side) {
    return '$side wins!';
  }

  @override
  String get resultYouWin => 'You win!';

  @override
  String resultYouLose(String name) {
    return '$name wins this time.';
  }

  @override
  String get resultDraw => 'Draw';

  @override
  String get reasonAmirCaptured => 'The Amir was captured.';

  @override
  String get reasonQalaTaken => 'A supplied piece took the enemy Qal\'a.';

  @override
  String get reasonNoLegalMoves => 'The opponent had no legal move.';

  @override
  String get reasonWaterVictory => 'Reached 10 water points.';

  @override
  String get reasonPlyLimit => 'Move limit reached.';

  @override
  String get playAgain => 'Play again';

  @override
  String get backHome => 'Home';

  @override
  String get nextOpponent => 'Next opponent';

  @override
  String get ladderLocked => 'Beat the previous opponent to unlock';

  @override
  String get ladderBeaten => 'Beaten';

  @override
  String ladderLevel(int level) {
    return 'Level $level';
  }

  @override
  String get ladderPlayAsSouth => 'Play (you move first)';

  @override
  String learnChapter(int number) {
    return 'Chapter $number';
  }

  @override
  String learnStars(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count stars',
      one: '1 star',
      zero: 'No stars yet',
    );
    return '$_temp0';
  }

  @override
  String get lessonNext => 'Next';

  @override
  String get lessonTryAgain => 'Not quite. Try again.';

  @override
  String get lessonShowAnswer => 'Show me';

  @override
  String get lessonComplete => 'Lesson complete!';

  @override
  String lessonStep(int current, int total) {
    return 'Step $current of $total';
  }

  @override
  String get errorGeneric => 'Something went wrong. Please try again.';

  @override
  String get retry => 'Retry';

  @override
  String get pieceAmir => 'Amir';

  @override
  String get pieceJundi => 'Jundi';

  @override
  String get pieceFaris => 'Faris';

  @override
  String get pieceRami => 'Rami';
}
