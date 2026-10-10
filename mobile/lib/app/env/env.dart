/// Build flavors.
enum Flavor { dev, staging, uat, prod }

/// Public configuration from `--dart-define-from-file=dart_defines/<flavor>.json`.
/// Never put secrets here: dart-defines can be extracted from the app.
class Env {
  const new({
    required this.flavor,
    required this.apiBaseUrl,
    required this.authIssuer,
    required this.authClientId,
    required this.onlineEnabled,
    required this.coachEnabled,
  });

  factory fromDefines(Flavor fallback) {
    const flavorName = String.fromEnvironment('FLAVOR');
    return Env(
      flavor: Flavor.values.firstWhere(
        (f) => f.name == flavorName,
        orElse: () => fallback,
      ),
      apiBaseUrl: const String.fromEnvironment(
        'API_BASE_URL',
        defaultValue: 'http://10.0.2.2:5000',
      ),
      authIssuer: const String.fromEnvironment('AUTH_ISSUER'),
      authClientId: const String.fromEnvironment(
        'AUTH_CLIENT_ID',
        defaultValue: 'qala-mobile',
      ),
      // Waits for backend Matches + Players hosts (docs/architecture.md §5).
      onlineEnabled: const bool.fromEnvironment('FEATURE_ONLINE'),
      // Waits for ai-service /ai-api/coach/review (docs/architecture.md §6).
      coachEnabled: const bool.fromEnvironment('FEATURE_COACH'),
    );
  }

  final Flavor flavor;
  final String apiBaseUrl;
  final String authIssuer;
  final String authClientId;
  final bool onlineEnabled;
  final bool coachEnabled;
}
