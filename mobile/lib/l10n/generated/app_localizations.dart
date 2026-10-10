import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'generated/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In en, this message translates to:
  /// **'Qal\'a'**
  String get appTitle;

  /// No description provided for @appTagline.
  ///
  /// In en, this message translates to:
  /// **'Fortresses, wells and water'**
  String get appTagline;

  /// No description provided for @homeLearnTitle.
  ///
  /// In en, this message translates to:
  /// **'Learn'**
  String get homeLearnTitle;

  /// No description provided for @homeLearnSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Short lessons, one idea each'**
  String get homeLearnSubtitle;

  /// No description provided for @homeLadderTitle.
  ///
  /// In en, this message translates to:
  /// **'AI ladder'**
  String get homeLadderTitle;

  /// No description provided for @homeLadderSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Beat eight opponents, one by one'**
  String get homeLadderSubtitle;

  /// No description provided for @homePassPlayTitle.
  ///
  /// In en, this message translates to:
  /// **'Two players'**
  String get homePassPlayTitle;

  /// No description provided for @homePassPlaySubtitle.
  ///
  /// In en, this message translates to:
  /// **'Play a friend on one device'**
  String get homePassPlaySubtitle;

  /// No description provided for @homeOnlineTitle.
  ///
  /// In en, this message translates to:
  /// **'Online'**
  String get homeOnlineTitle;

  /// No description provided for @homeOnlineSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Coming soon'**
  String get homeOnlineSubtitle;

  /// No description provided for @settingsTitle.
  ///
  /// In en, this message translates to:
  /// **'Settings'**
  String get settingsTitle;

  /// No description provided for @settingsLanguage.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get settingsLanguage;

  /// No description provided for @settingsTheme.
  ///
  /// In en, this message translates to:
  /// **'Theme'**
  String get settingsTheme;

  /// No description provided for @settingsThemeSystem.
  ///
  /// In en, this message translates to:
  /// **'System'**
  String get settingsThemeSystem;

  /// No description provided for @settingsThemeLight.
  ///
  /// In en, this message translates to:
  /// **'Light'**
  String get settingsThemeLight;

  /// No description provided for @settingsThemeDark.
  ///
  /// In en, this message translates to:
  /// **'Dark'**
  String get settingsThemeDark;

  /// No description provided for @settingsCoachTips.
  ///
  /// In en, this message translates to:
  /// **'Coach tips during games'**
  String get settingsCoachTips;

  /// No description provided for @sideSouth.
  ///
  /// In en, this message translates to:
  /// **'South'**
  String get sideSouth;

  /// No description provided for @sideNorth.
  ///
  /// In en, this message translates to:
  /// **'North'**
  String get sideNorth;

  /// No description provided for @turnOf.
  ///
  /// In en, this message translates to:
  /// **'{side} to move'**
  String turnOf(String side);

  /// No description provided for @yourTurn.
  ///
  /// In en, this message translates to:
  /// **'Your turn'**
  String get yourTurn;

  /// No description provided for @aiThinking.
  ///
  /// In en, this message translates to:
  /// **'{name} is thinking…'**
  String aiThinking(String name);

  /// No description provided for @moveCounter.
  ///
  /// In en, this message translates to:
  /// **'Move {ply}/{limit}'**
  String moveCounter(int ply, int limit);

  /// No description provided for @waterLabel.
  ///
  /// In en, this message translates to:
  /// **'Water {value}/{target}'**
  String waterLabel(int value, int target);

  /// No description provided for @undo.
  ///
  /// In en, this message translates to:
  /// **'Undo'**
  String get undo;

  /// No description provided for @restart.
  ///
  /// In en, this message translates to:
  /// **'New game'**
  String get restart;

  /// No description provided for @resign.
  ///
  /// In en, this message translates to:
  /// **'Resign'**
  String get resign;

  /// No description provided for @hintNoWater.
  ///
  /// In en, this message translates to:
  /// **'No water: this piece is cut off. It can move, but it cannot capture.'**
  String get hintNoWater;

  /// No description provided for @hintNotAllowed.
  ///
  /// In en, this message translates to:
  /// **'That move is not allowed.'**
  String get hintNotAllowed;

  /// No description provided for @resultWin.
  ///
  /// In en, this message translates to:
  /// **'{side} wins!'**
  String resultWin(String side);

  /// No description provided for @resultYouWin.
  ///
  /// In en, this message translates to:
  /// **'You win!'**
  String get resultYouWin;

  /// No description provided for @resultYouLose.
  ///
  /// In en, this message translates to:
  /// **'{name} wins this time.'**
  String resultYouLose(String name);

  /// No description provided for @resultDraw.
  ///
  /// In en, this message translates to:
  /// **'Draw'**
  String get resultDraw;

  /// No description provided for @reasonAmirCaptured.
  ///
  /// In en, this message translates to:
  /// **'The Amir was captured.'**
  String get reasonAmirCaptured;

  /// No description provided for @reasonQalaTaken.
  ///
  /// In en, this message translates to:
  /// **'A supplied piece took the enemy Qal\'a.'**
  String get reasonQalaTaken;

  /// No description provided for @reasonNoLegalMoves.
  ///
  /// In en, this message translates to:
  /// **'The opponent had no legal move.'**
  String get reasonNoLegalMoves;

  /// No description provided for @reasonWaterVictory.
  ///
  /// In en, this message translates to:
  /// **'Reached 10 water points.'**
  String get reasonWaterVictory;

  /// No description provided for @reasonPlyLimit.
  ///
  /// In en, this message translates to:
  /// **'Move limit reached.'**
  String get reasonPlyLimit;

  /// No description provided for @playAgain.
  ///
  /// In en, this message translates to:
  /// **'Play again'**
  String get playAgain;

  /// No description provided for @backHome.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get backHome;

  /// No description provided for @nextOpponent.
  ///
  /// In en, this message translates to:
  /// **'Next opponent'**
  String get nextOpponent;

  /// No description provided for @ladderLocked.
  ///
  /// In en, this message translates to:
  /// **'Beat the previous opponent to unlock'**
  String get ladderLocked;

  /// No description provided for @ladderBeaten.
  ///
  /// In en, this message translates to:
  /// **'Beaten'**
  String get ladderBeaten;

  /// No description provided for @ladderLevel.
  ///
  /// In en, this message translates to:
  /// **'Level {level}'**
  String ladderLevel(int level);

  /// No description provided for @ladderPlayAsSouth.
  ///
  /// In en, this message translates to:
  /// **'Play (you move first)'**
  String get ladderPlayAsSouth;

  /// No description provided for @learnChapter.
  ///
  /// In en, this message translates to:
  /// **'Chapter {number}'**
  String learnChapter(int number);

  /// No description provided for @learnStars.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =0{No stars yet} =1{1 star} other{{count} stars}}'**
  String learnStars(int count);

  /// No description provided for @lessonNext.
  ///
  /// In en, this message translates to:
  /// **'Next'**
  String get lessonNext;

  /// No description provided for @lessonTryAgain.
  ///
  /// In en, this message translates to:
  /// **'Not quite. Try again.'**
  String get lessonTryAgain;

  /// No description provided for @lessonShowAnswer.
  ///
  /// In en, this message translates to:
  /// **'Show me'**
  String get lessonShowAnswer;

  /// No description provided for @lessonComplete.
  ///
  /// In en, this message translates to:
  /// **'Lesson complete!'**
  String get lessonComplete;

  /// No description provided for @lessonStep.
  ///
  /// In en, this message translates to:
  /// **'Step {current} of {total}'**
  String lessonStep(int current, int total);

  /// No description provided for @errorGeneric.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get errorGeneric;

  /// No description provided for @retry.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get retry;

  /// No description provided for @pieceAmir.
  ///
  /// In en, this message translates to:
  /// **'Amir'**
  String get pieceAmir;

  /// No description provided for @pieceJundi.
  ///
  /// In en, this message translates to:
  /// **'Jundi'**
  String get pieceJundi;

  /// No description provided for @pieceFaris.
  ///
  /// In en, this message translates to:
  /// **'Faris'**
  String get pieceFaris;

  /// No description provided for @pieceRami.
  ///
  /// In en, this message translates to:
  /// **'Rami'**
  String get pieceRami;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
