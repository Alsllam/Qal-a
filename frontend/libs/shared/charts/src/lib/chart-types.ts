import type {
  BarSeriesOption,
  LineSeriesOption,
  PieSeriesOption,
} from 'echarts/charts';
import type {
  GridComponentOption,
  LegendComponentOption,
  MarkLineComponentOption,
  TooltipComponentOption,
} from 'echarts/components';
import type { ComposeOption } from 'echarts/core';

/** The only ECharts option type features see (type-only import: no runtime cost). */
export type ChartOption = ComposeOption<
  | BarSeriesOption
  | LineSeriesOption
  | PieSeriesOption
  | GridComponentOption
  | LegendComponentOption
  | TooltipComponentOption
  | MarkLineComponentOption
>;

export interface ChartDatum {
  label: string;
  value: number;
  color?: string;
}

export interface ChartSeries {
  name: string;
  data: (number | null)[];
  color?: string;
}

export type ValueFormatter = (value: number) => string;
