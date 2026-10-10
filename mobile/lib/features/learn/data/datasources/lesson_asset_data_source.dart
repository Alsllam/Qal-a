import 'dart:convert';

import 'package:flutter/services.dart';

import 'package:qala/core/domain/localized_text.dart';
import 'package:qala/features/learn/domain/entities/lesson.dart';

/// Lessons shipped as data in `assets/lessons/lessons.json` (GDD §4).
class LessonAssetDataSource {
  new(this._bundle);

  final AssetBundle _bundle;
  static const path = 'assets/lessons/lessons.json';

  Future<List<Chapter>> load() async => parse(await _bundle.loadString(path));

  static List<Chapter> parse(String source) {
    final json = jsonDecode(source) as Map<String, dynamic>;
    return [
      for (final c in json['chapters'] as List<dynamic>)
        _chapter(c as Map<String, dynamic>),
    ];
  }

  static Chapter _chapter(Map<String, dynamic> c) => Chapter(
    number: c['number'] as int,
    title: _text(c['title']),
    lessons: [
      for (final l in c['lessons'] as List<dynamic>)
        Lesson(
          id: (l as Map<String, dynamic>)['id'] as String,
          title: _text(l['title']),
          steps: [
            for (final s in l['steps'] as List<dynamic>)
              LessonStep(
                text: _text((s as Map<String, dynamic>)['text']),
                position: s['position'] as String,
                expected: [
                  for (final m in s['expected'] as List<dynamic>) m as String,
                ],
                success: s['success'] == null ? null : _text(s['success']),
              ),
          ],
        ),
    ],
  );

  static LocalizedText _text(Object? raw) {
    final m = raw! as Map<String, dynamic>;
    return LocalizedText(en: m['en'] as String, ar: m['ar'] as String);
  }
}
