/// Route paths. Use these constants, never string literals in widgets.
abstract final class Routes {
  static const splash = '/splash';
  static const home = '/';
  static const learn = '/learn';
  static const ladder = '/ladder';
  static const passAndPlay = '/play';
  static const settings = '/settings';

  static String lesson(String id) => '/learn/lesson/$id';
  static String ladderGame(int level) => '/ladder/$level';
}
