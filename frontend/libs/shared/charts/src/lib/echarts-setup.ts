/**
 * ECharts with only the modules the console uses. Loaded lazily by
 * ngx-echarts (see provideQalaCharts), so it stays out of the initial bundle.
 */
import { BarChart, LineChart, PieChart } from 'echarts/charts';
import {
  GridComponent,
  LegendComponent,
  MarkLineComponent,
  TooltipComponent,
} from 'echarts/components';
import * as echarts from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';
import { buildBrandTheme } from './brand-theme';

echarts.use([
  BarChart,
  LineChart,
  PieChart,
  GridComponent,
  LegendComponent,
  TooltipComponent,
  MarkLineComponent,
  CanvasRenderer,
]);
echarts.registerTheme('brand-light', buildBrandTheme('light'));
echarts.registerTheme('brand-dark', buildBrandTheme('dark'));

export default echarts;
