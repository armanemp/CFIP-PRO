import type { IChartApi } from "lightweight-charts";
import type { Candle } from "./types";
import { addIndicatorSeries } from "./chart-engine";
import { renderIndicatorRuntime } from "./indicator-runtime";

export function renderRegisteredIndicators(
  chart: IChartApi,
  candles: Candle[],
  selected: readonly string[],
  oscillatorPaneIndex = 1,
  parameters: Readonly<Record<string, Record<string, number>>> = {},
): void {
  const add = (
    data: Parameters<typeof addIndicatorSeries>[1],
    color: string,
    title: string,
    pane = 0,
  ) => addIndicatorSeries(chart, data, color, title, pane);

  for (const id of selected) {
    renderIndicatorRuntime(id, {
      candles,
      paneIndex: oscillatorPaneIndex,
      add,
    }, parameters[id]);
  }
}
