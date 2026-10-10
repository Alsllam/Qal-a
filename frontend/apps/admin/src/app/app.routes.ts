import { Route } from '@angular/router';
import { authGuard } from '@qala-fe/Core';
import {
  ApplicationLayoutComponent,
  StatusPageComponent,
} from '@qala-fe/theme-shared';

export const appRoutes: Route[] = [
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    component: ApplicationLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('@qala-fe/DashboardUiCommon').then(
            (m) => m.DashboardUICommonRoutes
          ),
      },
      {
        path: 'players',
        loadChildren: () =>
          import('@qala-fe/PlayersUiCommon').then(
            (m) => m.PlayerUICommonRoutes
          ),
      },
      {
        path: 'matches',
        loadChildren: () =>
          import('@qala-fe/MatchesUiCommon').then((m) => m.MatchUICommonRoutes),
      },
      { path: '403', component: StatusPageComponent, data: { code: 403 } },
      { path: '404', component: StatusPageComponent, data: { code: 404 } },
    ],
  },
  { path: '**', redirectTo: '/404' },
];
