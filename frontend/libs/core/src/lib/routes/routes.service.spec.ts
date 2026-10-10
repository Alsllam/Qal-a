import { TestBed } from '@angular/core/testing';
import { PermissionService } from '../permissions/permission.service';
import { RoutesService } from './routes.service';

describe('RoutesService', () => {
  it('shows only permitted entries, sorted by order', () => {
    const routes = TestBed.inject(RoutesService);
    routes.add([
      { path: '/b', name: 'B', order: 2, requiredPolicy: 'P.B' },
      { path: '/a', name: 'A', order: 1 },
      { path: '/c', name: 'C', order: 3, requiredPolicy: 'P.C' },
    ]);
    TestBed.inject(PermissionService).setGrantedPolicies(['P.C']);
    expect(routes.visible().map((r) => r.name)).toEqual(['A', 'C']);
  });
});
