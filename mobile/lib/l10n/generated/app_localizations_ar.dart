// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appTitle => 'قلعة';

  @override
  String get appTagline => 'قلاع وآبار وماء';

  @override
  String get homeLearnTitle => 'تعلّم';

  @override
  String get homeLearnSubtitle => 'دروس قصيرة، فكرة واحدة في كل درس';

  @override
  String get homeLadderTitle => 'سلّم التحدي';

  @override
  String get homeLadderSubtitle => 'تغلّب على ثمانية خصوم، واحدًا تلو الآخر';

  @override
  String get homePassPlayTitle => 'لاعبان';

  @override
  String get homePassPlaySubtitle => 'العب مع صديق على جهاز واحد';

  @override
  String get homeOnlineTitle => 'عبر الإنترنت';

  @override
  String get homeOnlineSubtitle => 'قريبًا';

  @override
  String get settingsTitle => 'الإعدادات';

  @override
  String get settingsLanguage => 'اللغة';

  @override
  String get settingsTheme => 'المظهر';

  @override
  String get settingsThemeSystem => 'حسب النظام';

  @override
  String get settingsThemeLight => 'فاتح';

  @override
  String get settingsThemeDark => 'داكن';

  @override
  String get settingsCoachTips => 'نصائح المدرّب أثناء اللعب';

  @override
  String get sideSouth => 'الجنوب';

  @override
  String get sideNorth => 'الشمال';

  @override
  String turnOf(String side) {
    return 'دور $side';
  }

  @override
  String get yourTurn => 'دورك';

  @override
  String aiThinking(String name) {
    return '$name يفكّر…';
  }

  @override
  String moveCounter(int ply, int limit) {
    return 'النقلة $ply/$limit';
  }

  @override
  String waterLabel(int value, int target) {
    return 'الماء $value/$target';
  }

  @override
  String get undo => 'تراجع';

  @override
  String get restart => 'لعبة جديدة';

  @override
  String get resign => 'استسلام';

  @override
  String get hintNoWater =>
      'لا ماء: هذه القطعة مقطوعة عن الإمداد. يمكنها التحرك لكن لا يمكنها الأسر.';

  @override
  String get hintNotAllowed => 'هذه النقلة غير مسموحة.';

  @override
  String resultWin(String side) {
    return 'فاز $side!';
  }

  @override
  String get resultYouWin => 'لقد فزت!';

  @override
  String resultYouLose(String name) {
    return 'فاز $name هذه المرة.';
  }

  @override
  String get resultDraw => 'تعادل';

  @override
  String get reasonAmirCaptured => 'تم أسر الأمير.';

  @override
  String get reasonQalaTaken => 'قطعة متصلة بالإمداد احتلت قلعة الخصم.';

  @override
  String get reasonNoLegalMoves => 'لم يبقَ للخصم أي نقلة.';

  @override
  String get reasonWaterVictory => 'وصل إلى 10 نقاط ماء.';

  @override
  String get reasonPlyLimit => 'انتهى عدد النقلات.';

  @override
  String get playAgain => 'العب مجددًا';

  @override
  String get backHome => 'الرئيسية';

  @override
  String get nextOpponent => 'الخصم التالي';

  @override
  String get ladderLocked => 'تغلّب على الخصم السابق لفتح هذا المستوى';

  @override
  String get ladderBeaten => 'تم التغلب عليه';

  @override
  String ladderLevel(int level) {
    return 'المستوى $level';
  }

  @override
  String get ladderPlayAsSouth => 'العب (تبدأ أنت)';

  @override
  String learnChapter(int number) {
    return 'الفصل $number';
  }

  @override
  String learnStars(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count نجمة',
      many: '$count نجمة',
      few: '$count نجوم',
      two: 'نجمتان',
      one: 'نجمة واحدة',
      zero: 'لا نجوم بعد',
    );
    return '$_temp0';
  }

  @override
  String get lessonNext => 'التالي';

  @override
  String get lessonTryAgain => 'ليس تمامًا. حاول مرة أخرى.';

  @override
  String get lessonShowAnswer => 'أرني الحل';

  @override
  String get lessonComplete => 'اكتمل الدرس!';

  @override
  String lessonStep(int current, int total) {
    return 'الخطوة $current من $total';
  }

  @override
  String get errorGeneric => 'حدث خطأ. حاول مرة أخرى.';

  @override
  String get retry => 'إعادة المحاولة';

  @override
  String get pieceAmir => 'الأمير';

  @override
  String get pieceJundi => 'الجندي';

  @override
  String get pieceFaris => 'الفارس';

  @override
  String get pieceRami => 'الرامي';
}
