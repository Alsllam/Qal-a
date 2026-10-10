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
  '0.1': const RuleVariant(RuleSet.standard, 'First complete ruleset'),
};
