import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:go_router/go_router.dart';

import 'package:qala/app/router/routes.dart';
import 'package:qala/app/theme/brand_tokens.g.dart';

/// Brand intro: the tower rises, then home (docs/brand-kit.md §2, motion).
class SplashPage extends StatefulWidget {
  const new({super.key});

  @override
  State<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends State<SplashPage>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: BrandMotion.logo,
  );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (MediaQuery.of(context).disableAnimations) {
      _controller.value = 1;
    }
    unawaited(
      _controller.forward().whenComplete(() {
        if (mounted) context.go(Routes.home);
      }),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final rise = CurvedAnimation(
      parent: _controller,
      curve: const Interval(0, .6, curve: Curves.easeOutCubic),
    );
    final fade = CurvedAnimation(
      parent: _controller,
      curve: const Interval(0, .4),
    );
    return Scaffold(
      body: Center(
        child: FadeTransition(
          opacity: fade,
          child: ScaleTransition(
            scale: Tween<double>(begin: .6, end: 1).animate(rise),
            alignment: Alignment.bottomCenter,
            child: Hero(
              tag: 'logo',
              child: SvgPicture.asset('assets/svg/logo_mark.svg', height: 120),
            ),
          ),
        ),
      ),
    );
  }
}
