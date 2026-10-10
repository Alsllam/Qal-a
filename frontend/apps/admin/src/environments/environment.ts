/** Development / demo builds. Runtime settings come from assets/app-settings.json. */
export const environment = {
  production: false,
  /** Mocks may only be switched on (by app-settings `useMockApi`) in non-production builds. */
  allowMockApi: true,
  remoteEnv: { url: 'assets/app-settings.json' },
};
