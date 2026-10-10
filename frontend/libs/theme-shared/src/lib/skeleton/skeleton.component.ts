import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

/** Inline loading placeholder: `<app-skeleton [lines]="3" />` or `[height]="120"` for a block. */
@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'aria-busy': 'true' },
  template: `
    @if (height()) {
    <span class="sk block" [style.height.px]="height()"></span>
    } @else { @for (l of lineArray(); track l) {
    <span
      class="sk line"
      [style.width.%]="l === lineArray().length - 1 ? 60 : 100"
    ></span>
    } }
  `,
  styles: `
    :host { display: flex; flex-direction: column; gap: 8px; }
    .sk { display: block; border-radius: var(--radius-sm); background: var(--surface-sunken); animation: pulse 1.2s ease-in-out infinite; }
    .line { height: 12px; }
    .block { width: 100%; border-radius: var(--radius-md); }
    @keyframes pulse { 50% { opacity: .45; } }
    @media (prefers-reduced-motion: reduce) { .sk { animation: none; } }
  `,
})
export class SkeletonComponent {
  readonly lines = input(3);
  readonly height = input<number | null>(null);
  readonly lineArray = computed(() =>
    Array.from({ length: this.lines() }, (_, i) => i)
  );
}
