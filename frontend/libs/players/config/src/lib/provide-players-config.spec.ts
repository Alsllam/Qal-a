import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PermissionService, RoutesService } from '@qala-fe/Core';
import { providePlayersConfig } from './provide-players-config';

describe('providePlayersConfig', () => {
  it('adds a menu entry guarded by Permissions.Players.ViewPlayer', async () => {
    TestBed.configureTestingModule({ providers: [providePlayersConfig()] });
    await TestBed.inject(ApplicationInitStatus).donePromise;
    const routes = TestBed.inject(RoutesService);
    const permissions = TestBed.inject(PermissionService);
    expect(routes.visible().map((r) => r.path)).not.toContain('/players');
    permissions.setGrantedPolicies(['Permissions.Players.ViewPlayer']);
    expect(routes.visible().map((r) => r.path)).toContain('/players');
  });
});
