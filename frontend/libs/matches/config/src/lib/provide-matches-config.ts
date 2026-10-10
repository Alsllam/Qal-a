import {
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { Permissions, RoutesService } from '@qala-fe/Core';

export function provideMatchesConfig() {
  return makeEnvironmentProviders([
    provideAppInitializer(() =>
      inject(RoutesService).add([
        {
          path: '/matches',
          name: 'Menu.Matches',
          icon: 'matches',
          order: 3,
          requiredPolicy: Permissions.Matches.ViewMatch,
        },
      ])
    ),
  ]);
}
