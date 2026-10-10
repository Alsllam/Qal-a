/** Shape of `assets/app-settings.json`. It is public: never put secrets here. */
export interface AppSettings {
  application: { name: string; baseUrl: string };
  oAuthConfig: OAuthSettings;
  /** One entry per backend host behind the BFF, keyed by `apiName`. */
  apis: Record<string, { url: string }>;
  /**
   * Development only: answer API calls from the in-app mock API.
   * Ignored by production builds (see `environment.allowMockApi`).
   */
  useMockApi?: boolean;
  defaultLanguage?: SupportedLanguage;
}

export interface OAuthSettings {
  issuer: string;
  clientId: string;
  responseType: 'code';
  scope: string;
  redirectUri?: string;
  requireHttps: boolean;
  showDebugInformation?: boolean;
}

export type SupportedLanguage = 'ar' | 'en';
