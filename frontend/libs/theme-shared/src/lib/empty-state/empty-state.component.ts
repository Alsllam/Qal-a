import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IconComponent, IconName } from '../icon/icon.component';

/** Brand illustration (a small fortress by a well) + text + optional call to action. */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="empty">
      @if (icon(); as name) {
      <div class="badge"><app-icon [name]="name" [size]="28" /></div>
      } @else {
      <svg class="art" viewBox="0 0 120 80" aria-hidden="true">
        <ellipse cx="60" cy="72" rx="54" ry="6" class="sand" />
        <path class="tower" d="M30 70V28h7v6h7v-6h7v6h7v-6h7v42z" />
        <path class="gate" d="M48 46s-6 7-6 11a6 6 0 0 0 12 0c0-4-6-11-6-11z" />
        <circle cx="92" cy="64" r="9" class="well" />
        <path
          class="drop"
          d="M92 52s-3 3.6-3 5.6a3 3 0 0 0 6 0c0-2-3-5.6-3-5.6z"
        />
      </svg>
      }
      <p class="title">{{ title() }}</p>
      @if (description()) {
      <p class="desc">{{ description() }}</p>
      }
      <ng-content />
    </div>
  `,
  styles: `
    .empty { display: flex; flex-direction: column; align-items: center; text-align: center; padding: 32px 16px; gap: 6px; }
    .art { width: 120px; height: 80px; margin-block-end: 8px; }
    .sand { fill: var(--surface-sunken); }
    .tower { fill: var(--brand-200); }
    :host-context([data-theme='dark']) .tower { fill: var(--brand-700); }
    .gate, .drop { fill: var(--water-500); }
    .well { fill: none; stroke: var(--water-300); stroke-width: 2; }
    .badge { width: 56px; height: 56px; border-radius: 50%; display: grid; place-items: center; background: var(--surface-sunken); color: var(--text-muted); margin-block-end: 8px; }
    .title { margin: 0; font-weight: 600; color: var(--text-primary); }
    .desc { margin: 0 0 8px; color: var(--text-muted); font-size: var(--fs-sm); max-width: 42ch; }
  `,
})
export class EmptyStateComponent {
  readonly title = input('');
  readonly description = input('');
  readonly icon = input<IconName | null>(null);
}
