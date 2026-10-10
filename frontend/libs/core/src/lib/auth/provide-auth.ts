import {
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import {
  OAuthModuleConfig,
  OAuthStorage,
  provideOAuthClient,
} from 'angular-oauth2-oidc';
import { APP_SETTINGS } from '../settings/app-settings';
import { AuthService } from './auth.service';

/**
 * OIDC client (code + PKCE) against the Auth host from app-settings. The
 * library interceptor attaches the access token to calls to the configured
 * API hosts only, so features never set `Authorization` by hand.
 */
export function provideQalaAuth() {
  return makeEnvironmentProviders([
    provideOAuthClient(),
    {
      provide: OAuthModuleConfig,
      useFactory: (): OAuthModuleConfig => {
        const settings = inject(APP_SETTINGS);
        return {
          resourceServer: {
            sendAccessToken: true,
            allowedUrls: Object.values(settings.apis).map((api) => api.url),
          },
        };
      },
    },
    { provide: OAuthStorage, useFactory: () => sessionStorage },
    provideAppInitializer(() => inject(AuthService).init()),
  ]);
}
