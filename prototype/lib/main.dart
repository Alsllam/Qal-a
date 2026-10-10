import 'package:flutter/material.dart';

import 'src/screens/home_screen.dart';

void main() => runApp(const QalaPrototypeApp());

class QalaPrototypeApp extends StatelessWidget {
  const QalaPrototypeApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: "Qal'a prototype",
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF8A6235),
          surface: const Color(0xFFF6EBD6),
        ),
        useMaterial3: true,
      ),
      home: const HomeScreen(),
    );
  }
}
