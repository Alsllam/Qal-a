import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { permissionGuard } from './permission.guard';
import { PermissionService } from './permission.service';

describe('permissionGuard', () => {
  const run = (policy?: string) =>
    TestBed.runInInjectionContext(() =>
      permissionGuard(
        {
          data: { requiredPolicy: policy },
        } as unknown as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot
      )
    );

  beforeEach(() =>
    TestBed.configureTestingModule({ providers: [provideRouter([])] })
  );

  it('allows granted policies', () => {
    TestBed.inject(PermissionService).setGrantedPolicies(['P']);
    expect(run('P')).toBe(true);
  });

  it('redirects to /403 otherwise', () => {
    const result = run('P');
    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/403');
  });
});
