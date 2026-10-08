import 'side.dart';
import 'square.dart';

/// Board part of the standard opening position (rank 7 first).
const String standardSetup = '1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1';

/// Every tunable rule parameter. The balance lab varies these; the app uses
/// [RuleSet.standard].
class RuleSet {
  const RuleSet({
    required this.version,
    this.plyLimit = 60,
    this.farisRange = 3,
    this.shotDistance = 2,
    this.amirIsSource = true,
    this.wells = const [Square(2, 3), Square(4, 3)],
    this.southQala = const Square(3, 0),
    this.northQala = const Square(3, 6),
    this.setup = standardSetup,
  });

  /// Rules version 0.1, as written in docs/rules.md.
  static const RuleSet standard = RuleSet(version: '0.1');

  /// Rules version label, recorded in docs/balance-log.md.
  final String version;

  /// The game ends after this many plies (half-moves) if nobody has won.
  final int plyLimit;

  /// Maximum slide distance of the Faris.
  final int farisRange;

  /// Exact distance of a Rami shot.
  final int shotDistance;

  /// Whether the Amir is a water source.
  final bool amirIsSource;

  /// Well squares.
  final List<Square> wells;

  final Square southQala;
  final Square northQala;

  /// Board part of the opening position in position notation.
  final String setup;

  Square qalaOf(Side side) => side == Side.south ? southQala : northQala;

  RuleSet copyWith({
    String? version,
    int? plyLimit,
    int? farisRange,
    int? shotDistance,
    bool? amirIsSource,
    List<Square>? wells,
    Square? southQala,
    Square? northQala,
    String? setup,
  }) =>
      RuleSet(
        version: version ?? this.version,
        plyLimit: plyLimit ?? this.plyLimit,
        farisRange: farisRange ?? this.farisRange,
        shotDistance: shotDistance ?? this.shotDistance,
        amirIsSource: amirIsSource ?? this.amirIsSource,
        wells: wells ?? this.wells,
        southQala: southQala ?? this.southQala,
        northQala: northQala ?? this.northQala,
        setup: setup ?? this.setup,
      );
}
