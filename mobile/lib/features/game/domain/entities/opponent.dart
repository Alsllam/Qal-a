import 'package:equatable/equatable.dart';

/// A named AI opponent on the ladder (docs/GDD.md §5). Engine settings are
/// provisional until calibrated by the balance lab and telemetry.
class Opponent extends Equatable {
  const new({
    required this.level,
    required this.nameEn,
    required this.nameAr,
    required this.depth,
    required this.noise,
  });

  final int level;
  final String nameEn;
  final String nameAr;
  final int depth;
  final double noise;

  String name(String languageCode) => languageCode == 'ar' ? nameAr : nameEn;

  @override
  List<Object?> get props => [level];
}

abstract final class Opponents {
  /// Levels 1–6 ship in the MVP; 7–8 need a stronger search (GDD §5).
  static const ladder = [
    Opponent(
      level: 1,
      nameEn: 'Shepherd',
      nameAr: 'الراعي',
      depth: 1,
      noise: 1.5,
    ),
    Opponent(
      level: 2,
      nameEn: 'Camel Driver',
      nameAr: 'الحادي',
      depth: 1,
      noise: 0.6,
    ),
    Opponent(
      level: 3,
      nameEn: 'Merchant',
      nameAr: 'التاجر',
      depth: 2,
      noise: 0.4,
    ),
    Opponent(
      level: 4,
      nameEn: 'Guard',
      nameAr: 'الحارس',
      depth: 2,
      noise: 0.15,
    ),
    Opponent(
      level: 5,
      nameEn: 'Commander',
      nameAr: 'القائد',
      depth: 3,
      noise: 0.2,
    ),
    Opponent(level: 6, nameEn: 'Amir', nameAr: 'الأمير', depth: 3, noise: 0.05),
  ];

  static Opponent byLevel(int level) =>
      ladder.firstWhere((o) => o.level == level);
}
