import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { prefersReducedMotion } from '@qala-fe/Core';
import { ChartComponent, ChartOption } from '@qala-fe/Charts';
import {
  IconComponent,
  IconName,
  SkeletonComponent,
} from '@qala-fe/theme-shared';

export type KpiTone = 'success' | 'warning' | 'danger' | 'neutral';

/** KPI tile: label, value that counts up once, comparison line and optional sparkline. */
@Component({
  selector: 'app-kpi-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ChartComponent, IconComponent, SkeletonComponent],
  template: `
    <article class="card kpi">
      <header>
        <span class="icon" [class]="'icon tone-' + tone()"
          ><app-icon [name]="icon()" [size]="18"
        /></span>
        <h3>{{ label() }}</h3>
      </header>
      @if (loading()) {
      <app-skeleton [lines]="2" />
      } @else {
      <p class="value num">{{ display() }}</p>
      <p class="compare">
        @if (badge()) {
        <span [class]="'status-tag status-tag-' + tone()">{{ badge() }}</span>
        }
        <span class="text-muted">{{ comparison() }}</span>
      </p>
      @if (sparkline(); as spark) {
      <app-chart class="spark" [options]="spark" [height]="40" />
      } }
    </article>
  `,
  styles: `
    :host { display: block; }
    .kpi { display: flex; flex-direction: column; gap: 6px; min-height: 148px; height: 100%; }
    header { display: flex; align-items: center; gap: 10px; }
    h3 { margin: 0; font-size: var(--fs-sm); font-weight: 600; color: var(--text-secondary); }
    .icon { width: 32px; height: 32px; border-radius: var(--radius-md); display: grid; place-items: center; flex: none; }
    .tone-success { background: var(--status-success-bg); color: var(--status-success-fg); }
    .tone-warning { background: var(--status-warning-bg); color: var(--status-warning-fg); }
    .tone-danger { background: var(--status-danger-bg); color: var(--status-danger-fg); }
    .tone-neutral { background: var(--status-neutral-bg); color: var(--status-neutral-fg); }
    .value { margin: 4px 0 0; font-size: var(--fs-3xl); font-weight: 700; line-height: 1.1; color: var(--text-primary); }
    .compare { margin: 0; display: flex; flex-wrap: wrap; align-items: center; gap: 6px; font-size: var(--fs-xs); }
    .spark { margin-block-start: auto; }
  `,
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly icon = input<IconName>('dashboard');
  readonly value = input<number | null>(null);
  readonly format = input<(value: number) => string>((v) => String(v));
  readonly comparison = input('');
  readonly badge = input('');
  readonly tone = input<KpiTone>('neutral');
  readonly loading = input(false);
  readonly sparkline = input<ChartOption | null>(null);

  readonly display = signal('—');
  private animated = false;
  private frame = 0;

  constructor() {
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(this.frame));
    effect(() => {
      const value = this.value();
      const format = this.format();
      if (value === null) return;
      untracked(() => this.show(value, format));
    });
  }

  /** Counts up once on first load; later updates (filters, language) are instant. */
  private show(value: number, format: (v: number) => string): void {
    cancelAnimationFrame(this.frame);
    if (
      this.animated ||
      prefersReducedMotion() ||
      typeof requestAnimationFrame === 'undefined'
    ) {
      this.display.set(format(value));
      return;
    }
    this.animated = true;
    const start = performance.now();
    const duration = 700;
    const step = (now: number) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      this.display.set(format(value * eased));
      if (t < 1) this.frame = requestAnimationFrame(step);
    };
    this.frame = requestAnimationFrame(step);
  }
}
