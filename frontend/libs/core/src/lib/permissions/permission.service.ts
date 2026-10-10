import { Injectable, computed, signal } from '@angular/core';

/**
 * Holds the granted policies of the signed-in user and evaluates policy
 * expressions such as `A || B && C` (`&&` binds tighter than `||`).
 */
@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly granted = signal<ReadonlySet<string>>(new Set());
  readonly grantedPolicies = computed(() => this.granted());

  setGrantedPolicies(policies: Iterable<string>): void {
    this.granted.set(new Set(policies));
  }

  clear(): void {
    this.granted.set(new Set());
  }

  /** Reactive when called inside a signal context (template, computed, effect). */
  getGrantedPolicy(expression: string | null | undefined): boolean {
    if (!expression || !expression.trim()) return true;
    const granted = this.granted();
    return expression.split('||').some((andGroup) =>
      andGroup
        .split('&&')
        .map((p) => p.trim())
        .filter(Boolean)
        .every((policy) => granted.has(policy))
    );
  }
}
