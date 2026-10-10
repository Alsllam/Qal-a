import {
  provideHttpClient,
  withInterceptors,
  withInterceptorsFromDi,
} from '@angular/common/http';
import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withViewTransitions,
} from '@angular/router';
import {
  AppSettings,
  MOCK_API_ENABLED,
  MOCK_API_HANDLER,
  MockApiHandler,
  mockApiInterceptor,
  provideAppSettings,
  provideQalaAuth,
  provideQalaLocalization,
} from '@qala-fe/Core';
import { provideQalaCharts } from '@qala-fe/Charts';
import { provideDashboardConfig } from '@qala-fe/DashboardConfig';
import { provideMatchesConfig } from '@qala-fe/MatchesConfig';
import { providePlayersConfig } from '@qala-fe/PlayersConfig';
import { provideErrorHandling } from '@qala-fe/theme-shared';
import { appRoutes } from './app.routes';

export function createAppConfig(
  settings: AppSettings,
  mockHandler?: MockApiHandler
): ApplicationConfig {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideZoneChangeDetection({ eventCoalescing: true }),
      provideAppSettings(settings),
      provideRouter(
        appRoutes,
        withComponentInputBinding(),
        withViewTransitions({ skipInitialTransition: true }),
        withInMemoryScrolling({ scrollPositionRestoration: 'top' })
      ),
      // Mock interceptor first, so mocked calls never reach the OAuth interceptor or network.
      provideHttpClient(
        withInterceptors([mockApiInterceptor]),
        withInterceptorsFromDi()
      ),
      ...(mockHandler
        ? [
            { provide: MOCK_API_HANDLER, useValue: mockHandler },
            { provide: MOCK_API_ENABLED, useValue: true },
          ]
        : []),
      provideQalaLocalization(),
      provideQalaAuth(),
      provideErrorHandling(),
      provideQalaCharts(),
      provideDashboardConfig(),
      providePlayersConfig(),
      provideMatchesConfig(),
    ],
  };
}
