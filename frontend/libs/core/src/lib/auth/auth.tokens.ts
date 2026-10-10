import { InjectionToken } from '@angular/core';

/** True only when the dev mock API is active (never in production builds). */
export const MOCK_API_ENABLED = new InjectionToken<boolean>(
  'MOCK_API_ENABLED',
  {
    providedIn: 'root',
    factory: () => false,
  }
);
