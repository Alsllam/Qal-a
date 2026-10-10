import 'package:flutter/material.dart';

import '../strings.dart';

/// Short rules summary in a bottom sheet.
Future<void> showRulesSheet(BuildContext context, S s) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (context) => Directionality(
      textDirection: s.isAr ? TextDirection.rtl : TextDirection.ltr,
      child: DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.75,
        builder: (context, controller) => ListView(
          controller: controller,
          padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
          children: [
            Text(s.howToPlay, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 12),
            for (final (title, body) in s.rulesSummary)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    Text(body),
                  ],
                ),
              ),
          ],
        ),
      ),
    ),
  );
}
