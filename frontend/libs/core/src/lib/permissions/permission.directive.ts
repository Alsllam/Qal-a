import {
  Directive,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  input,
} from '@angular/core';
import { PermissionService } from './permission.service';

/**
 * Renders the element only when the policy expression is granted.
 * Usage: `<button *appPermission="'Permissions.Players.ManagePlayer'">`.
 */
@Directive({ selector: '[appPermission]' })
export class PermissionDirective {
  readonly appPermission = input<string | null | undefined>();

  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly permissions = inject(PermissionService);
  private rendered = false;

  constructor() {
    effect(() => {
      const allowed = this.permissions.getGrantedPolicy(this.appPermission());
      if (allowed && !this.rendered) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.viewContainer.clear();
        this.rendered = false;
      }
    });
  }
}
