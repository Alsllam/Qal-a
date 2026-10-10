export const environment = {
  production: true,
  /** Production builds never load the mock API, whatever app-settings.json says. */
  allowMockApi: false,
  remoteEnv: { url: 'assets/app-settings.json' },
};
