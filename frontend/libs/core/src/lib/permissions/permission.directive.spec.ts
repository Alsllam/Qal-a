import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PermissionDirective } from './permission.directive';
import { PermissionService } from './permission.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PermissionDirective],
  template: `
    <button *appPermission="'Permissions.Players.ManagePlayer'" class="manage">
      Ban
    </button>
    <span
      *appPermission="'Permissions.X || Permissions.Players.ViewPlayer'"
      class="view"
      >View</span
    >
  `,
})
class HostComponent {}

describe('PermissionDirective', () => {
  function render() {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('hides elements whose policy is not granted', () => {
    TestBed.inject(PermissionService).setGrantedPolicies([]);
    const el: HTMLElement = render().nativeElement;
    expect(el.querySelector('.manage')).toBeNull();
    expect(el.querySelector('.view')).toBeNull();
  });

  it('shows elements whose policy expression is granted', () => {
    TestBed.inject(PermissionService).setGrantedPolicies([
      'Permissions.Players.ViewPlayer',
    ]);
    const el: HTMLElement = render().nativeElement;
    expect(el.querySelector('.manage')).toBeNull();
    expect(el.querySelector('.view')).not.toBeNull();
  });

  it('reacts when permissions change after render', () => {
    const permissions = TestBed.inject(PermissionService);
    permissions.setGrantedPolicies([]);
    const fixture = render();
    permissions.setGrantedPolicies(['Permissions.Players.ManagePlayer']);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.manage')).not.toBeNull();
    permissions.clear();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.manage')).toBeNull();
  });
});
