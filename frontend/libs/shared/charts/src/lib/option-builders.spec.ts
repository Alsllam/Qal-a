import {
  BRAND_SERIES_COLORS,
  BRAND_SERIES_COLORS_DARK,
  buildBrandTheme,
  fontFamilyFor,
} from './brand-theme';
import { withChartDefaults, withRtl } from './chart-layout';
import {
  barOption,
  donutOption,
  kpiSparkline,
  lineOption,
} from './option-builders';

type Obj = Record<string, unknown>;
const series = (o: unknown) => (o as Obj)['series'] as Obj[];

describe('chart option builders', () => {
  it('barOption maps categories and leaves colours to the theme unless given', () => {
    const o = barOption({
      categories: ['0–9', '10–19'],
      series: [
        { name: 'Players', data: [3, 5] },
        { name: 'AI', data: [4, 4], color: '#E0A21B' },
      ],
    }) as Obj;
    expect((o['xAxis'] as Obj)['data']).toEqual(['0–9', '10–19']);
    expect(series(o)[0]['itemStyle']).toBeUndefined();
    expect((series(o)[1]['itemStyle'] as Obj)['color']).toBe('#E0A21B');
    expect(o['legend']).toBeDefined();
  });

  it('barOption swaps axes when horizontal', () => {
    const o = barOption({
      categories: ['a'],
      series: [{ name: 's', data: [1] }],
      horizontal: true,
    }) as Obj;
    expect((o['yAxis'] as Obj)['type']).toBe('category');
    expect((o['xAxis'] as Obj)['type']).toBe('value');
    expect(o['legend']).toBeUndefined();
  });

  it('lineOption adds reference lines as dashed flat series', () => {
    const o = lineOption({
      categories: ['d1', 'd2', 'd3'],
      series: [{ name: 'South', data: [0.5, 0.55, 0.52] }],
      referenceLines: [{ name: 'AI baseline', value: 0.542 }],
    });
    const s = series(o);
    expect(s).toHaveLength(2);
    expect(s[1]['data']).toEqual([0.542, 0.542, 0.542]);
    expect((s[1]['lineStyle'] as Obj)['type']).toBe('dashed');
  });

  it('value formatter is used for axis labels and tooltips', () => {
    const fmt = (v: number) => `${Math.round(v * 100)}%`;
    const o = lineOption({
      categories: ['a'],
      series: [{ name: 's', data: [0.5] }],
      valueFormatter: fmt,
    }) as Obj;
    const axisFormatter = ((o['yAxis'] as Obj)['axisLabel'] as Obj)[
      'formatter'
    ] as (v: number) => string;
    const tooltipFormatter = (o['tooltip'] as Obj)['valueFormatter'] as (
      v: unknown
    ) => string;
    expect(axisFormatter(0.25)).toBe('25%');
    expect(tooltipFormatter(0.5)).toBe('50%');
    expect(tooltipFormatter(undefined)).toBe('—');
  });

  it('donutOption drops zero slices', () => {
    const o = donutOption({
      items: [
        { label: 'Water', value: 10 },
        { label: 'Timeout', value: 0 },
      ],
    });
    expect((series(o)[0]['data'] as Obj[]).map((d) => d['name'])).toEqual([
      'Water',
    ]);
  });

  it('kpiSparkline hides axes', () => {
    const o = kpiSparkline([1, 2, 3]) as Obj;
    expect((o['xAxis'] as Obj)['show']).toBe(false);
    expect((o['yAxis'] as Obj)['show']).toBe(false);
    expect(series(o)[0]['data']).toEqual([1, 2, 3]);
  });
});

describe('RTL and defaults', () => {
  it('withRtl inverts the x axis and moves the value axis right', () => {
    const o = withRtl(
      lineOption({
        categories: ['a', 'b'],
        series: [{ name: 's', data: [1, 2] }],
      }),
      true
    ) as Obj;
    expect((o['xAxis'] as Obj)['inverse']).toBe(true);
    expect((o['yAxis'] as Obj)['position']).toBe('right');
  });

  it('withRtl leaves LTR untouched', () => {
    const base = barOption({
      categories: ['a'],
      series: [{ name: 's', data: [1] }],
    });
    expect(withRtl(base, false)).toBe(base);
  });

  it('withRtl mirrors an off-centre donut and its side legend', () => {
    const o = withRtl(
      donutOption({ items: [{ label: 'x', value: 1 }] }),
      true
    ) as Obj;
    expect(series(o)[0]['center']).toEqual(['65%', '50%']);
    expect((o['legend'] as Obj)['left']).toBe(0);
  });

  it('withChartDefaults sets font and turns animation off for reduced motion', () => {
    const o = withChartDefaults(kpiSparkline([1]), {
      rtl: false,
      fontFamily: 'X',
      reducedMotion: true,
    }) as Obj;
    expect(o['animation']).toBe(false);
    expect((o['textStyle'] as Obj)['fontFamily']).toBe('X');
  });

  it('brand themes use the token series colours and differ per mode', () => {
    const light = buildBrandTheme('light');
    const dark = buildBrandTheme('dark');
    expect(light['color']).toEqual([...BRAND_SERIES_COLORS]);
    expect(dark['color']).toEqual([...BRAND_SERIES_COLORS_DARK]);
    expect(light['tooltip']).not.toEqual(dark['tooltip']);
    expect(fontFamilyFor('ar').startsWith("'IBM Plex Sans Arabic'")).toBe(true);
  });
});
