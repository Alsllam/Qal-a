// Default entry for `flutter run` without a flavor: same as main_dev.dart.
import 'package:qala/app/env/env.dart';
import 'package:qala/bootstrap.dart';

void main() => bootstrap(Flavor.dev);
