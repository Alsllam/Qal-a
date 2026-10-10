import 'package:game_core/game_core.dart';

/// A named rules version tried in the balance lab.
class RuleVariant {
  const RuleVariant(this.rules, this.summary);

  final RuleSet rules;

  /// One-line description of what changed from the previous version.
  final String summary;

  String get version => rules.version;
}

/// Every rules version tried, in order. Entries are never removed, so old
/// reports can be reproduced.
final Map<String, RuleVariant> ruleVariants = {
  '0.1': const RuleVariant(RuleSet(version: '0.1'), 'First complete ruleset'),
  '0.2': const RuleVariant(
    RuleSet(version: '0.2', waterToWin: 8),
    'Water points: +1 per Well held at the start of your turn; 8 wins. '
    'Ply limit decided by water first',
  ),
  '0.2b': const RuleVariant(
    RuleSet(version: '0.2b', waterToWin: 6),
    'As 0.2 with 6 water points to win',
  ),
  '0.3': const RuleVariant(
    RuleSet(
      version: '0.3',
      waterToWin: 10,
      wellsAreSources: false,
      waterNeedsSupply: true,
    ),
    'Wells are no longer supply sources and score only while supplied '
    '(linked to your Qal\'a or Amir); 10 water points to win',
  ),
  '0.3b': const RuleVariant(
    RuleSet(
      version: '0.3b',
      waterToWin: 10,
      wellsAreSources: false,
      waterNeedsSupply: true,
      northStartWater: 1,
    ),
    'As 0.3, and North starts with 1 water point',
  ),
  '0.4': const RuleVariant(
    RuleSet(
      version: '0.4',
      waterToWin: 10,
      wellsAreSources: false,
      waterNeedsSupply: true,
      amirIsSource: false,
    ),
    'As 0.3, and the Amir is no longer a water source: all water comes '
    'from your Qal\'a',
  ),
  '0.5': const RuleVariant(
    RuleSet(
      version: '0.5',
      waterToWin: 10,
      wellsAreSources: false,
      waterNeedsSupply: true,
      amirEarnsWater: false,
    ),
    'As 0.3, and an Amir standing on a Well earns no water (stops the '
    'Rami-guarded Amir camping on a Well)',
  ),
  '0.5b': const RuleVariant(
    RuleSet(
      version: '0.5b',
      waterToWin: 10,
      wellsAreSources: false,
      waterNeedsSupply: true,
      amirEarnsWater: false,
      northStartWater: 1,
    ),
    'As 0.5, and North starts with 1 water point',
  ),
  '0.6': const RuleVariant(
    RuleSet(
      version: '0.6',
      waterToWin: 10,
      wellsAreSources: false,
      waterNeedsSupply: true,
      amirEarnsWater: false,
      diagonalShots: false,
    ),
    'As 0.5, and the Rami shoots orthogonally only (4 directions, not 8)',
  ),
  '0.7': const RuleVariant(
    RuleSet(
      version: '0.7',
      waterToWin: 10,
      wellsAreSources: false,
      waterNeedsSupply: true,
      amirEarnsWater: false,
      diagonalShots: false,
      northStartWater: 1,
    ),
    'As 0.6, and North starts with 1 water point (offsets the first-move '
    'tempo toward the Wells)',
  ),
};
