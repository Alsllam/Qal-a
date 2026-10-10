import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from './permission.service';

/** Reads `data.requiredPolicy` from the route; denied → `/403`. */
export const permissionGuard: CanActivateFn = (route) => {
  const policy = route.data?.['requiredPolicy'] as string | undefined;
  return inject(PermissionService).getGrantedPolicy(policy)
    ? true
    : inject(Router).parseUrl('/403');
};
