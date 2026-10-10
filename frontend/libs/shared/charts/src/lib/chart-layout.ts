import { ChartOption } from './chart-types';

type AxisLike = Record<string, unknown> & {
  type?: string;
  position?: string;
  inverse?: boolean;
};

function mapAxes(axes: unknown, fn: (axis: AxisLike) => AxisLike): unknown {
  if (!axes) return axes;
  return Array.isArray(axes)
    ? axes.map((a) => fn(a as AxisLike))
    : fn(axes as AxisLike);
}

/**
 * Mirrors a cartesian chart for RTL: the category axis runs right-to-left,
 * the value axis sits on the right, a side legend moves to the left.
 */
export function withRtl(option: ChartOption, rtl: boolean): ChartOption {
  if (!rtl) return option;
  const o: Record<string, unknown> = { ...option };
  // Both axis types flip: categories run right-to-left, horizontal bars grow leftwards.
  o['xAxis'] = mapAxes(o['xAxis'], (a) =>
    a['show'] === false ? a : { ...a, inverse: !a.inverse }
  );
  o['yAxis'] = mapAxes(o['yAxis'], (a) => ({ ...a, position: 'right' }));
  const legend = o['legend'] as Record<string, unknown> | undefined;
  if (legend && legend['right'] !== undefined && legend['left'] === undefined) {
    o['legend'] = { ...legend, left: legend['right'], right: undefined };
  }
  const series = o['series'];
  if (Array.isArray(series)) {
    o['series'] = series.map((s: Record<string, unknown>) => {
      const center = s['center'];
      if (
        s['type'] === 'pie' &&
        Array.isArray(center) &&
        typeof center[0] === 'string' &&
        center[0] !== '50%'
      ) {
        const pct = 100 - parseFloat(center[0]);
        return { ...s, center: [`${pct}%`, center[1]] };
      }
      return s;
    });
  }
  return o as ChartOption;
}

/** Font, animation and RTL defaults applied by <app-chart>. */
export function withChartDefaults(
  option: ChartOption,
  opts: { rtl: boolean; fontFamily: string; reducedMotion: boolean }
): ChartOption {
  const mirrored = withRtl(option, opts.rtl) as Record<string, unknown>;
  const tooltip = mirrored['tooltip'] as Record<string, unknown> | undefined;
  return {
    ...mirrored,
    textStyle: { fontFamily: opts.fontFamily },
    tooltip: tooltip
      ? {
          ...tooltip,
          textStyle: { fontFamily: opts.fontFamily },
          extraCssText: `direction:${
            opts.rtl ? 'rtl' : 'ltr'
          };text-align:start;border-radius:8px;box-shadow:0 4px 12px rgba(21,27,64,.16);`,
        }
      : tooltip,
    animation: !opts.reducedMotion,
    animationDuration: 600,
    animationEasing: 'cubicOut',
  } as ChartOption;
}
