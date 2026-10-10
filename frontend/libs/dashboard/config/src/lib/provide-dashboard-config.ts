import {
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { Permissions, RoutesService } from '@qala-fe/Core';

export function provideDashboardConfig() {
  return makeEnvironmentProviders([
    provideAppInitializer(() =>
      inject(RoutesService).add([
        {
          path: '/dashboard',
          name: 'Menu.Dashboard',
          icon: 'dashboard',
          order: 1,
          requiredPolicy: Permissions.Dashboard.ViewBalance,
        },
      ])
    ),
  ]);
}
