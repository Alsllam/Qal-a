import {
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { Permissions, RoutesService } from '@qala-fe/Core';

export function providePlayersConfig() {
  return makeEnvironmentProviders([
    provideAppInitializer(() =>
      inject(RoutesService).add([
        {
          path: '/players',
          name: 'Menu.Players',
          icon: 'players',
          order: 2,
          requiredPolicy: Permissions.Players.ViewPlayer,
        },
      ])
    ),
  ]);
}
