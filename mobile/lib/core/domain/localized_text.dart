import 'package:equatable/equatable.dart';

/// A text in Arabic and English.
class LocalizedText extends Equatable {
  const new({required this.en, required this.ar});

  final String en;
  final String ar;

  String of(String languageCode) => languageCode == 'ar' ? ar : en;

  @override
  List<Object?> get props => [en, ar];
}
