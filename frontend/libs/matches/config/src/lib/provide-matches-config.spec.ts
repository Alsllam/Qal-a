import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PermissionService, RoutesService } from '@qala-fe/Core';
import { provideMatchesConfig } from './provide-matches-config';

describe('provideMatchesConfig', () => {
  it('adds a menu entry guarded by Permissions.Matches.ViewMatch', async () => {
    TestBed.configureTestingModule({ providers: [provideMatchesConfig()] });
    await TestBed.inject(ApplicationInitStatus).donePromise;
    const routes = TestBed.inject(RoutesService);
    const permissions = TestBed.inject(PermissionService);
    expect(routes.visible().map((r) => r.path)).not.toContain('/matches');
    permissions.setGrantedPolicies(['Permissions.Matches.ViewMatch']);
    expect(routes.visible().map((r) => r.path)).toContain('/matches');
  });
});
