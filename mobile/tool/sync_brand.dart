// Copies the shared brand kit (../brand) into the app. Run after changing
// brand/tokens.json:  dart run tool/sync_brand.dart
import 'dart:io';

void main() {
  const copies = {
    '../brand/dist/app_tokens.g.dart': 'lib/app/theme/brand_tokens.g.dart',
    '../brand/png/app-icon-1024.png': 'assets/icon/app_icon.png',
    '../brand/png/app-icon-foreground-1024.png': 'assets/icon/app_icon_foreground.png',
    '../brand/png/app-icon-monochrome-1024.png': 'assets/icon/app_icon_monochrome.png',
    '../brand/png/logo-mark-512.png': 'assets/icon/splash_logo.png',
    '../brand/logo/logo-mark.svg': 'assets/svg/logo_mark.svg',
    '../brand/logo/logo-mono.svg': 'assets/svg/logo_mono.svg',
  };
  for (final MapEntry(key: from, value: to) in copies.entries) {
    File(to).parent.createSync(recursive: true);
    File(from).copySync(to);
    stdout.writeln('$from -> $to');
  }
}
