import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PermissionDirective } from '@qala-fe/Core';
import { TranslatePipe } from '@ngx-translate/core';
import { IconComponent, IconName } from '../icon/icon.component';

export enum PageHeaderActionTypes {
  Primary = 'primary',
  Secondary = 'secondary',
}

export interface PageHeaderAction {
  actionName: string;
  type: PageHeaderActionTypes;
  permission?: string;
  icon?: IconName;
  callback: () => void;
}

export interface Breadcrumb {
  /** Translation key or literal text (when `raw`). */
  label: string;
  link?: string;
  raw?: boolean;
}

@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, RouterLink, PermissionDirective, IconComponent],
  template: `
    <header class="page-header">
      <div class="titles">
        @if (breadcrumbs().length) {
        <nav
          class="crumbs"
          [attr.aria-label]="'General.Breadcrumb' | translate"
        >
          @for (b of breadcrumbs(); track b.label; let last = $last) { @if
          (b.link && !last) {
          <a [routerLink]="b.link">{{
            b.raw ? b.label : (b.label | translate)
          }}</a>
          <span class="sep" aria-hidden="true">/</span>
          } @else {
          <span>{{ b.raw ? b.label : (b.label | translate) }}</span>
          } }
        </nav>
        }
        <h1>{{ titleRaw() ? title() : (title() | translate) }}</h1>
        @if (subtitle()) {
        <p class="subtitle">{{ subtitle() | translate }}</p>
        }
      </div>
      <div class="actions">
        <ng-content />
        @for (a of actions(); track a.actionName) {
        <button
          *appPermission="a.permission"
          type="button"
          class="btn"
          [class.btn-primary]="a.type === 'primary'"
          [class.btn-secondary]="a.type === 'secondary'"
          (click)="a.callback()"
        >
          @if (a.icon) {
          <app-icon [name]="a.icon" [size]="18" />
          }
          {{ a.actionName | translate }}
        </button>
        }
      </div>
    </header>
  `,
  styles: `
    .page-header { display: flex; flex-wrap: wrap; align-items: flex-end; justify-content: space-between; gap: 12px 16px; margin-block-end: 20px; }
    .titles { min-width: 0; }
    h1 { margin: 0; font-size: var(--fs-2xl); line-height: 1.25; font-weight: 700; color: var(--text-primary); }
    .subtitle { margin: 4px 0 0; color: var(--text-muted); font-size: var(--fs-sm); }
    .crumbs { display: flex; gap: 6px; font-size: var(--fs-sm); color: var(--text-muted); margin-block-end: 4px; flex-wrap: wrap; }
    .crumbs a { color: var(--primary); text-decoration: none; }
    .crumbs a:hover { text-decoration: underline; }
    .actions { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; }
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly titleRaw = input(false);
  readonly subtitle = input('');
  readonly breadcrumbs = input<Breadcrumb[]>([]);
  readonly actions = input<PageHeaderAction[]>([]);
}
