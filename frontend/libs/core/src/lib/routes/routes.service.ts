import { Injectable, computed, inject, signal } from '@angular/core';
import { PermissionService } from '../permissions/permission.service';

export interface MenuItem {
  path: string;
  /** Translation key, e.g. `Menu.Players`. */
  name: string;
  icon?: MenuIcon;
  order: number;
  parentName?: string;
  requiredPolicy?: string;
}

export type MenuIcon =
  | 'dashboard'
  | 'players'
  | 'matches'
  | 'content'
  | 'settings';

export interface MenuNode extends MenuItem {
  children: MenuNode[];
}

/** Sidebar entries contributed by each feature's `provide{Feature}Config()`. */
@Injectable({ providedIn: 'root' })
export class RoutesService {
  private readonly permissions = inject(PermissionService);
  private readonly items = signal<MenuItem[]>([]);

  /** Tree of entries the current user may see, sorted by `order`. */
  readonly visible = computed<MenuNode[]>(() => {
    const allowed = this.items().filter((i) =>
      this.permissions.getGrantedPolicy(i.requiredPolicy)
    );
    const build = (parent?: string): MenuNode[] =>
      allowed
        .filter((i) => i.parentName === parent)
        .sort((a, b) => a.order - b.order)
        .map((i) => ({ ...i, children: build(i.name) }));
    return build(undefined);
  });

  add(items: MenuItem[]): void {
    this.items.update((current) => [
      ...current.filter((c) => !items.some((i) => i.name === c.name)),
      ...items,
    ]);
  }
}
