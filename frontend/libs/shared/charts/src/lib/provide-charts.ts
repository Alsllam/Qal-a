import { provideEchartsCore } from 'ngx-echarts';

/** Registers ngx-echarts with the lazily loaded, tree-shaken ECharts build. */
export function provideQalaCharts() {
  return provideEchartsCore({
    echarts: () => import('./echarts-setup').then((m) => m.default),
  });
}
