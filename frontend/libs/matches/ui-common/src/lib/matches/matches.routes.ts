import { Route } from '@angular/router';
import { Permissions, permissionGuard } from '@qala-fe/Core';

export const MatchUICommonRoutes: Route[] = [
  {
    path: '',
    loadComponent: () =>
      import('./matches-list.component').then((m) => m.MatchesListComponent),
    canActivate: [permissionGuard],
    data: { requiredPolicy: Permissions.Matches.ViewMatch },
  },
  {
    path: 'view/:id',
    loadComponent: () =>
      import('./match-view.component').then((m) => m.MatchViewComponent),
    canActivate: [permissionGuard],
    data: { requiredPolicy: Permissions.Matches.ViewMatch, isReadOnly: true },
  },
];
