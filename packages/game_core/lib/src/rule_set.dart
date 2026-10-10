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
    this.waterToWin,
    this.wellsAreSources = true,
    this.waterNeedsSupply = false,
    this.northStartWater = 0,
    this.amirEarnsWater = true,
    this.diagonalShots = true,
  });

  /// The current rules, as written in docs/rules.md (version 0.6, chosen by
  /// the balance lab; see docs/balance-log.md).
  static const RuleSet standard = RuleSet(
    version: '0.6',
    waterToWin: 10,
    wellsAreSources: false,
    waterNeedsSupply: true,
    amirEarnsWater: false,
    diagonalShots: false,
  );

  /// The first ruleset, kept for reference and tests.
  static const RuleSet v0_1 = RuleSet(version: '0.1');

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

  /// Water points needed to win, or null if the rule is off. At the start of
  /// each of your turns you gain 1 water point per Well you hold.
  final int? waterToWin;

  /// Whether a piece standing on a Well is a water source for supply.
  final bool wellsAreSources;

  /// Whether a Well earns water points only while the piece on it is
  /// supplied.
  final bool waterNeedsSupply;

  /// Water points North starts with (compensation for moving second).
  final int northStartWater;

  /// Whether an Amir standing on a Well earns water points.
  final bool amirEarnsWater;

  /// Whether the Rami may shoot diagonally (otherwise orthogonally only).
  final bool diagonalShots;

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
    int? Function()? waterToWin,
    bool? wellsAreSources,
    bool? waterNeedsSupply,
    int? northStartWater,
    bool? amirEarnsWater,
    bool? diagonalShots,
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
        waterToWin: waterToWin == null ? this.waterToWin : waterToWin(),
        wellsAreSources: wellsAreSources ?? this.wellsAreSources,
        waterNeedsSupply: waterNeedsSupply ?? this.waterNeedsSupply,
        northStartWater: northStartWater ?? this.northStartWater,
        amirEarnsWater: amirEarnsWater ?? this.amirEarnsWater,
        diagonalShots: diagonalShots ?? this.diagonalShots,
      );
}
