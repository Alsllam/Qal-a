import { Route } from '@angular/router';
import { Permissions, permissionGuard } from '@qala-fe/Core';

export const DashboardUICommonRoutes: Route[] = [
  {
    path: '',
    loadComponent: () =>
      import('./balance-dashboard.component').then(
        (m) => m.BalanceDashboardComponent
      ),
    canActivate: [permissionGuard],
    data: { requiredPolicy: Permissions.Dashboard.ViewBalance },
  },
];
