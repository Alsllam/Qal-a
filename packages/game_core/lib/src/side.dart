/// The two players. South moves first.
enum Side {
  south('s'),
  north('n');

  const Side(this.code);

  /// Single-letter code used in position notation.
  final String code;

  Side get opponent => this == south ? north : south;

  static Side fromCode(String code) => switch (code) {
        's' => south,
        'n' => north,
        _ => throw FormatException('Unknown side "$code"'),
      };
}
