import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import {
  LocalizationService,
  ThemeModeService,
  prefersReducedMotion,
} from '@qala-fe/Core';
import { TranslatePipe } from '@ngx-translate/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { ChartThemeName, fontFamilyFor } from './brand-theme';
import { withChartDefaults } from './chart-layout';
import { ChartOption } from './chart-types';

/**
 * `<app-chart [options] [loading] [empty] [height]>`: the only way features
 * render ECharts. Applies the brand theme for the colour mode, RTL mirroring,
 * fonts and reduced motion, and shows a skeleton / empty state.
 */
@Component({
  selector: 'app-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgxEchartsDirective, TranslatePipe],
  template: `
    @if (loading()) {
    <div
      class="chart-skeleton"
      [style.height.px]="height()"
      aria-busy="true"
    ></div>
    } @else if (empty() || !options()) {
    <div class="chart-empty" [style.height.px]="height()">
      <svg viewBox="0 0 48 48" aria-hidden="true">
        <path d="M6 40h36M12 34V22M20 34V14M28 34v-8M36 34V18" />
      </svg>
      <span>{{ emptyText() || ('Charts.NoData' | translate) }}</span>
    </div>
    } @else { @for (theme of [themeName()]; track theme) {
    <div
      echarts
      class="chart"
      [style.height.px]="height()"
      [options]="finalOptions()!"
      [theme]="theme"
      [autoResize]="true"
      role="img"
      [attr.aria-label]="ariaLabel()"
    ></div>
    } }
  `,
  styles: `
    :host { display: block; }
    .chart { width: 100%; }
    .chart-skeleton { border-radius: var(--radius-md); background: linear-gradient(90deg, var(--surface-sunken) 0%, var(--surface-raised) 50%, var(--surface-sunken) 100%);
      background-size: 200% 100%; animation: chart-shimmer 1.4s linear infinite; opacity: .7; }
    .chart-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; color: var(--text-muted); font-size: var(--fs-sm); }
    .chart-empty svg { width: 40px; height: 40px; fill: none; stroke: var(--border-subtle); stroke-width: 3; stroke-linecap: round; }
    @keyframes chart-shimmer { from { background-position: 200% 0; } to { background-position: -200% 0; } }
    @media (prefers-reduced-motion: reduce) { .chart-skeleton { animation: none; } }
  `,
})
export class ChartComponent {
  private readonly l10n = inject(LocalizationService);
  private readonly themeMode = inject(ThemeModeService);

  readonly options = input<ChartOption | null>(null);
  readonly loading = input(false);
  readonly empty = input(false);
  readonly emptyText = input<string>('');
  readonly height = input(280);
  readonly ariaLabel = input<string>('');

  readonly themeName = computed<ChartThemeName>(() =>
    this.themeMode.isDark() ? 'brand-dark' : 'brand-light'
  );

  readonly finalOptions = computed(() => {
    const option = this.options();
    if (!option) return null;
    return withChartDefaults(option, {
      rtl: this.l10n.isRtl(),
      fontFamily: fontFamilyFor(this.l10n.currentLang()),
      reducedMotion: prefersReducedMotion(),
    });
  });
}
