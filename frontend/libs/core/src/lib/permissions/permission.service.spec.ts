import { TestBed } from '@angular/core/testing';
import { PermissionService } from './permission.service';

describe('PermissionService', () => {
  let service: PermissionService;

  beforeEach(() => {
    service = TestBed.inject(PermissionService);
    service.setGrantedPolicies(['A', 'B']);
  });

  it.each([
    ['A', true],
    ['C', false],
    ['A && B', true],
    ['A && C', false],
    ['C || B', true],
    ['C || D', false],
    ['C && D || A', true],
    ['', true],
    [null, true],
    [undefined, true],
  ])('%p → %p', (expr, expected) => {
    expect(service.getGrantedPolicy(expr)).toBe(expected);
  });

  it('clear() revokes everything', () => {
    service.clear();
    expect(service.getGrantedPolicy('A')).toBe(false);
  });
});
