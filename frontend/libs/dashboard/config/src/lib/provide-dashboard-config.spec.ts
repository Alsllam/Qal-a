import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PermissionService, RoutesService } from '@qala-fe/Core';
import { provideDashboardConfig } from './provide-dashboard-config';

describe('provideDashboardConfig', () => {
  it('adds a menu entry guarded by Permissions.Dashboard.ViewBalance', async () => {
    TestBed.configureTestingModule({ providers: [provideDashboardConfig()] });
    await TestBed.inject(ApplicationInitStatus).donePromise;
    const routes = TestBed.inject(RoutesService);
    const permissions = TestBed.inject(PermissionService);
    expect(routes.visible().map((r) => r.path)).not.toContain('/dashboard');
    permissions.setGrantedPolicies(['Permissions.Dashboard.ViewBalance']);
    expect(routes.visible().map((r) => r.path)).toContain('/dashboard');
  });
});
