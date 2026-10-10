import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LocalizationService, ThemeModeService } from '@qala-fe/Core';
import {
  BRAND_PALETTE,
  ChartComponent,
  ChartDatum,
  ChartOption,
  barOption,
  donutOption,
  kpiSparkline,
  lineOption,
} from '@qala-fe/Charts';
import {
  BalanceStatsDto,
  EndReason,
  MatchesService,
} from '@qala-fe/MatchesProxy';
import { PageHeaderComponent } from '@qala-fe/theme-shared';
import { TranslatePipe } from '@ngx-translate/core';
import {
  AI_BASELINES,
  BALANCE_TARGETS,
  END_REASON_GROUPS,
  endReasonGroup,
} from './ai-baseline';
import { KpiCardComponent, KpiTone } from './kpi-card.component';

/** End reasons are coloured by meaning: water reasons use the (reserved) water hue. */
function reasonColors(dark: boolean): Partial<Record<EndReason, string>> {
  const p = BRAND_PALETTE;
  return {
    waterVictory: p.water500,
    plyLimitWater: p.water300,
    amirCaptured: dark ? p.brand300 : p.brand500,
    qalaTaken: dark ? p.accent400 : p.accent500,
    plyLimitWells: dark ? p.brand200 : p.brand300,
    plyLimitMaterial: p.neutral400,
    plyLimitDraw: p.neutral300,
    resign: p.success500,
    timeout: p.danger500,
    abandon: dark ? p.neutral500 : p.neutral600,
  };
}

/** Live balance dashboard: human results per rules version vs the AI baseline. */
@Component({
  selector: 'app-balance-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageHeaderComponent,
    ChartComponent,
    KpiCardComponent,
    TranslatePipe,
  ],
  templateUrl: './balance-dashboard.component.html',
  styleUrl: './balance-dashboard.component.scss',
})
export class BalanceDashboardComponent {
  private readonly matches = inject(MatchesService);
  readonly l10n = inject(LocalizationService);
  private readonly theme = inject(ThemeModeService);

  readonly versions = Object.keys(AI_BASELINES);
  readonly periods = [7, 30, 90];
  readonly rulesVersion = signal('0.6');
  readonly periodDays = signal(30);

  readonly stats = rxResource<
    BalanceStatsDto,
    { from: string; to: string; rulesVersion: string }
  >({
    params: () => {
      const to = new Date();
      const from = new Date(
        to.getTime() - (this.periodDays() - 1) * 86_400_000
      );
      return {
        from: isoDate(from),
        to: isoDate(to),
        rulesVersion: this.rulesVersion(),
      };
    },
    stream: ({ params }) => this.matches.getStats(params),
  });

  readonly loading = computed(() => this.stats.isLoading());
  readonly data = computed(() =>
    this.stats.hasValue() ? this.stats.value() : null
  );
  readonly baseline = computed(
    () => AI_BASELINES[this.rulesVersion()] ?? AI_BASELINES['0.6']
  );
  readonly drawRate = computed(() => {
    const d = this.data();
    return d && d.games ? d.draws / d.games : null;
  });

  private t(key: string, params?: Record<string, unknown>): string {
    this.l10n.currentLang(); // re-evaluate on language change
    return this.l10n.instant(key, params);
  }
  readonly pct = (v: number) => this.l10n.formatPercent(v, 1);
  readonly int = (v: number) => this.l10n.formatNumber(Math.round(v));
  readonly dec = (v: number) =>
    this.l10n.formatNumber(v, {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    });

  // ---- KPI cards -------------------------------------------------------
  readonly southKpi = computed(() => {
    const d = this.data();
    const b = this.baseline();
    if (!d) return null;
    const delta = (d.southScore - b.southScore) * 100;
    const ok =
      Math.abs(d.southScore - BALANCE_TARGETS.southScore.center) <=
      BALANCE_TARGETS.southScore.tolerance;
    return {
      tone: (ok ? 'success' : 'warning') as KpiTone,
      badge: this.t(ok ? 'Dashboard.WithinTarget' : 'Dashboard.OutsideTarget'),
      comparison: this.t('Dashboard.VsAi', {
        value: this.pct(b.southScore),
        delta: this.signed(delta, 'pp'),
      }),
    };
  });

  readonly drawKpi = computed(() => {
    const rate = this.drawRate();
    const b = this.baseline();
    if (rate === null) return null;
    const ok = rate <= BALANCE_TARGETS.maxDrawRate;
    return {
      tone: (ok ? 'success' : 'danger') as KpiTone,
      badge: this.t(ok ? 'Dashboard.WithinTarget' : 'Dashboard.OutsideTarget'),
      comparison: this.t('Dashboard.VsAi', {
        value: this.pct(b.drawRate),
        delta: this.signed((rate - b.drawRate) * 100, 'pp'),
      }),
    };
  });

  readonly lengthKpi = computed(() => {
    const d = this.data();
    const b = this.baseline();
    if (!d) return null;
    const ok =
      d.meanPlies >= BALANCE_TARGETS.meanPlies.min &&
      d.meanPlies <= BALANCE_TARGETS.meanPlies.max;
    return {
      tone: (ok ? 'success' : 'warning') as KpiTone,
      badge: this.t(ok ? 'Dashboard.WithinTarget' : 'Dashboard.OutsideTarget'),
      comparison: this.t('Dashboard.VsAiPlies', {
        value: this.dec(b.meanPlies),
        delta: this.signed(d.meanPlies - b.meanPlies, ''),
      }),
    };
  });

  readonly gamesKpi = computed(() => {
    const d = this.data();
    if (!d) return null;
    const perDay = d.byDay.length ? d.games / d.byDay.length : 0;
    return {
      comparison: this.t('Dashboard.PerDay', { value: this.dec(perDay) }),
      sparkline: kpiSparkline(d.byDay.map((x) => x.games)),
    };
  });

  // ---- Charts ----------------------------------------------------------
  readonly southByDay = computed<ChartOption | null>(() => {
    const d = this.data();
    if (!d) return null;
    return lineOption({
      categories: d.byDay.map((x) =>
        this.l10n.formatDate(x.date, { day: 'numeric', month: 'short' })
      ),
      series: [
        {
          name: this.t('Dashboard.SouthScore'),
          data: d.byDay.map((x) => (x.games ? x.southScore : null)),
        },
      ],
      referenceLines: [
        {
          name: this.t('Dashboard.AiBaseline'),
          value: this.baseline().southScore,
          color: this.theme.isDark()
            ? BRAND_PALETTE.accent400
            : BRAND_PALETTE.accent500,
        },
        {
          name: this.t('Dashboard.Target'),
          value: BALANCE_TARGETS.southScore.center,
          color: BRAND_PALETTE.success500,
        },
      ],
      area: true,
      yMin: 0.3,
      yMax: 0.7,
      valueFormatter: this.pct,
      axisFormatter: (v) => this.l10n.formatPercent(v, 0),
    });
  });

  readonly endReasonItems = computed<ChartDatum[]>(() => {
    const d = this.data();
    if (!d) return [];
    const colors = reasonColors(this.theme.isDark());
    return [...d.endReasons]
      .sort((a, b) => b.count - a.count)
      .map((r) => ({
        label: this.t('EndReason.' + r.reason),
        value: r.count,
        color: colors[r.reason],
      }));
  });

  readonly endReasonsDonut = computed<ChartOption | null>(() =>
    this.data()
      ? donutOption({
          items: this.endReasonItems(),
          valueFormatter: this.int,
          legendBottom: true,
        })
      : null
  );

  readonly endReasonsVsAi = computed<ChartOption | null>(() => {
    const d = this.data();
    if (!d || !d.games) return null;
    const groups = Object.fromEntries(
      END_REASON_GROUPS.map((g) => [g, 0])
    ) as Record<string, number>;
    for (const r of d.endReasons) groups[endReasonGroup(r.reason)] += r.count;
    const b = this.baseline();
    return barOption({
      categories: END_REASON_GROUPS.map((g) =>
        this.t('Dashboard.ReasonGroup.' + g)
      ),
      series: [
        // Theme order: brand first, accent second (light/dark aware).
        {
          name: this.t('Dashboard.Players'),
          data: END_REASON_GROUPS.map((g) => groups[g] / d.games),
        },
        {
          name: this.t('Dashboard.AiBaseline'),
          data: END_REASON_GROUPS.map((g) => b.endReasons[g]),
        },
      ],
      horizontal: true,
      valueFormatter: this.pct,
      axisFormatter: (v) => this.l10n.formatPercent(v, 0),
    });
  });

  readonly lengthHistogram = computed<ChartOption | null>(() => {
    const d = this.data();
    if (!d) return null;
    return barOption({
      categories: d.lengthHistogram.map((h) =>
        this.l10n.localizeDigits(
          h.fromPly === h.toPly ? `${h.fromPly}` : `${h.fromPly}–${h.toPly}`
        )
      ),
      series: [
        {
          name: this.t('Dashboard.Games'),
          data: d.lengthHistogram.map((h) => h.count),
        },
      ],
      valueFormatter: this.int,
    });
  });

  readonly lengthSubtitle = computed(() => {
    const d = this.data();
    if (!d) return '';
    return this.t('Dashboard.LengthSubtitle', {
      mean: this.dec(d.meanPlies),
      median: this.int(d.medianPlies),
      ai: this.dec(this.baseline().meanPlies),
    });
  });

  setVersion(v: string): void {
    this.rulesVersion.set(v);
  }

  setPeriod(days: string): void {
    this.periodDays.set(Number(days));
  }

  private signed(value: number, unit: string): string {
    const sign = value > 0 ? '+' : value < 0 ? '−' : '±';
    const text = this.l10n.formatNumber(Math.abs(value), {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    });
    return `${sign}${text}${
      unit ? ' ' + this.t('Dashboard.Unit.' + unit) : ''
    }`;
  }
}

function isoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}
