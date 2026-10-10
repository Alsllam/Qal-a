import { brandTokens } from './generated/brand-tokens';

export type ChartThemeName = 'brand-light' | 'brand-dark';

export const BRAND_SERIES_COLORS: readonly string[] = brandTokens.chartSeries;

/**
 * Dark mode uses the same order with lighter, slightly desaturated steps
 * (indigo 500 has too little contrast on the indigo night surface).
 */
export const BRAND_SERIES_COLORS_DARK: readonly string[] = [
  brandTokens.palette.brand300,
  brandTokens.palette.accent400,
  brandTokens.palette.water300,
  brandTokens.palette.success500,
  brandTokens.palette.danger500,
  brandTokens.palette.brand200,
  brandTokens.palette.neutral400,
];

export function seriesColors(mode: 'light' | 'dark'): readonly string[] {
  return mode === 'dark' ? BRAND_SERIES_COLORS_DARK : BRAND_SERIES_COLORS;
}

export function fontFamilyFor(lang: string): string {
  const { arabic, latin } = brandTokens.fonts;
  return lang === 'ar'
    ? `'${arabic}', '${latin}', system-ui, sans-serif`
    : `'${latin}', '${arabic}', system-ui, sans-serif`;
}

/** ECharts theme built from the brand tokens (brand/dist/tokens.ts). */
export function buildBrandTheme(
  mode: 'light' | 'dark'
): Record<string, unknown> {
  const t = brandTokens.themes[mode];
  const p = brandTokens.palette;
  const axisLine = mode === 'light' ? p.neutral300 : '#3A4275';
  const splitLine = t.borderSubtle;
  const axis = {
    axisLine: { show: true, lineStyle: { color: axisLine } },
    axisTick: { show: false },
    axisLabel: { color: t.textMuted },
    splitLine: { show: true, lineStyle: { color: splitLine, type: 'dashed' } },
    nameTextStyle: { color: t.textMuted },
  };
  return {
    color: [...seriesColors(mode)],
    backgroundColor: 'transparent',
    textStyle: { color: t.textSecondary },
    title: {
      textStyle: { color: t.textPrimary },
      subtextStyle: { color: t.textMuted },
    },
    legend: {
      textStyle: { color: t.textSecondary },
      pageTextStyle: { color: t.textSecondary },
    },
    tooltip: {
      backgroundColor: t.surfaceRaised,
      borderColor: t.borderSubtle,
      borderWidth: 1,
      textStyle: { color: t.textPrimary },
      extraCssText: `border-radius:${brandTokens.radius.md}px;box-shadow:0 4px 12px rgba(21,27,64,.16);`,
    },
    categoryAxis: { ...axis, splitLine: { show: false } },
    valueAxis: { ...axis, axisLine: { show: false } },
    line: {
      symbol: 'circle',
      symbolSize: 6,
      smooth: false,
      lineStyle: { width: 2 },
    },
    bar: { itemStyle: { borderRadius: [4, 4, 0, 0] } },
    pie: { itemStyle: { borderColor: t.surface, borderWidth: 2 } },
    markLine: { lineStyle: { color: t.textMuted } },
  };
}

/** Brand palette (hex), for features that colour series by meaning (e.g. water). */
export const BRAND_PALETTE = brandTokens.palette;
