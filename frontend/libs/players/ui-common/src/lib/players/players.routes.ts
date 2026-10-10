import { Route } from '@angular/router';
import { Permissions, permissionGuard } from '@qala-fe/Core';

export const PlayerUICommonRoutes: Route[] = [
  {
    path: '',
    loadComponent: () =>
      import('./players-list.component').then((m) => m.PlayersListComponent),
    canActivate: [permissionGuard],
    data: { requiredPolicy: Permissions.Players.ViewPlayer },
  },
];
