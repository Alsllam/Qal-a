import {
  ChartDatum,
  ChartOption,
  ChartSeries,
  ValueFormatter,
} from './chart-types';

const identity: ValueFormatter = (v) => String(v);

export interface CartesianInput {
  categories: string[];
  series: ChartSeries[];
  valueFormatter?: ValueFormatter;
  /** Formatter for value-axis labels (defaults to valueFormatter). */
  axisFormatter?: ValueFormatter;
  yMin?: number;
  yMax?: number;
}

export interface ReferenceLine {
  name: string;
  value: number;
  color?: string;
}

function tooltipFormatter(fmt: ValueFormatter) {
  return (value: unknown) => (typeof value === 'number' ? fmt(value) : '—');
}

export function barOption(
  input: CartesianInput & { horizontal?: boolean; stacked?: boolean }
): ChartOption {
  const fmt = input.valueFormatter ?? identity;
  const category = { type: 'category' as const, data: input.categories };
  const value = {
    type: 'value' as const,
    min: input.yMin,
    max: input.yMax,
    axisLabel: { formatter: (v: number) => (input.axisFormatter ?? fmt)(v) },
  };
  return {
    grid: {
      left: 16,
      right: 16,
      top: input.series.length > 1 ? 36 : 16,
      bottom: 8,
      containLabel: true,
    },
    legend: input.series.length > 1 ? { top: 0, icon: 'roundRect' } : undefined,
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      valueFormatter: tooltipFormatter(fmt),
    },
    xAxis: input.horizontal ? value : category,
    yAxis: input.horizontal ? category : value,
    series: input.series.map((s) => ({
      type: 'bar' as const,
      name: s.name,
      data: s.data,
      stack: input.stacked ? 'total' : undefined,
      barMaxWidth: 28,
      // No colour → the brand theme picks it (light/dark aware).
      itemStyle: s.color ? { color: s.color } : undefined,
    })),
  };
}

export function lineOption(
  input: CartesianInput & { area?: boolean; referenceLines?: ReferenceLine[] }
): ChartOption {
  const fmt = input.valueFormatter ?? identity;
  const marks = input.referenceLines ?? [];
  return {
    grid: {
      left: 16,
      right: 16,
      top: input.series.length > 1 || marks.length ? 36 : 16,
      bottom: 8,
      containLabel: true,
    },
    legend:
      input.series.length > 1 || marks.length
        ? { top: 0, icon: 'roundRect' }
        : undefined,
    tooltip: { trigger: 'axis', valueFormatter: tooltipFormatter(fmt) },
    xAxis: { type: 'category', data: input.categories, boundaryGap: false },
    yAxis: {
      type: 'value',
      min: input.yMin,
      max: input.yMax,
      axisLabel: { formatter: (v: number) => (input.axisFormatter ?? fmt)(v) },
    },
    series: [
      ...input.series.map((s) => ({
        type: 'line' as const,
        name: s.name,
        data: s.data,
        showSymbol: s.data.length <= 31,
        itemStyle: s.color ? { color: s.color } : undefined,
        lineStyle: { width: 2, ...(s.color ? { color: s.color } : {}) },
        areaStyle: input.area
          ? { opacity: 0.12, ...(s.color ? { color: s.color } : {}) }
          : undefined,
      })),
      // Reference lines are drawn as flat series so they appear in the legend.
      ...marks.map((m) => ({
        type: 'line' as const,
        name: m.name,
        data: input.categories.map(() => m.value),
        showSymbol: false,
        silent: true,
        itemStyle: m.color ? { color: m.color } : undefined,
        lineStyle: {
          width: 1.5,
          type: 'dashed' as const,
          ...(m.color ? { color: m.color } : {}),
        },
        tooltip: { show: false },
      })),
    ],
  };
}

export function donutOption(input: {
  items: ChartDatum[];
  valueFormatter?: ValueFormatter;
  /** Show the legend below the ring (better on narrow cards). */
  legendBottom?: boolean;
}): ChartOption {
  const fmt = input.valueFormatter ?? identity;
  return {
    tooltip: {
      trigger: 'item',
      valueFormatter: tooltipFormatter(fmt),
    },
    // A plain legend wraps onto several rows instead of paging.
    legend: input.legendBottom
      ? {
          bottom: 0,
          left: 'center',
          icon: 'circle',
          itemGap: 12,
          itemWidth: 10,
          itemHeight: 10,
        }
      : {
          orient: 'vertical',
          right: 0,
          top: 'middle',
          icon: 'circle',
          type: 'scroll',
        },
    series: [
      {
        type: 'pie',
        radius: input.legendBottom ? ['40%', '62%'] : ['52%', '78%'],
        center: input.legendBottom ? ['50%', '36%'] : ['35%', '50%'],
        avoidLabelOverlap: true,
        label: { show: false },
        labelLine: { show: false },
        data: input.items
          .filter((d) => d.value > 0)
          .map((d) => ({
            name: d.label,
            value: d.value,
            itemStyle: d.color ? { color: d.color } : undefined,
          })),
      },
    ],
  };
}

export function kpiSparkline(values: number[], color?: string): ChartOption {
  return {
    grid: { left: 0, right: 0, top: 2, bottom: 2 },
    xAxis: {
      type: 'category',
      show: false,
      data: values.map((_, i) => String(i)),
      boundaryGap: false,
    },
    yAxis: { type: 'value', show: false, scale: true },
    tooltip: { show: false },
    series: [
      {
        type: 'line',
        data: values,
        showSymbol: false,
        silent: true,
        lineStyle: { width: 2, ...(color ? { color } : {}) },
        areaStyle: { opacity: 0.12, ...(color ? { color } : {}) },
      },
    ],
  };
}
