import { InjectionToken, makeEnvironmentProviders } from '@angular/core';
import { AppSettings } from './app-settings.model';

export const APP_SETTINGS = new InjectionToken<AppSettings>('APP_SETTINGS');

/**
 * Loads the runtime configuration before bootstrap, so every provider can read
 * it synchronously. The build is environment-agnostic: deployment replaces the
 * JSON file.
 */
export async function loadAppSettings(url: string): Promise<AppSettings> {
  const response = await fetch(url, { cache: 'no-cache' });
  if (!response.ok) {
    throw new Error(`Cannot load ${url}: HTTP ${response.status}`);
  }
  return (await response.json()) as AppSettings;
}

export function provideAppSettings(settings: AppSettings) {
  return makeEnvironmentProviders([
    { provide: APP_SETTINGS, useValue: settings },
  ]);
}
