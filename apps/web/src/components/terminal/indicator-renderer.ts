import type { IChartApi, ISeriesApi, SeriesType } from "lightweight-charts";
import type { Candle } from "./types";
import { getIndicatorDefinition, normalizeIndicatorParameters, type IndicatorId } from "./indicator-registry";
import { atr, ema, bollinger, sma, wma, vwap, obv, rsi, macd, dmi, stochastic, donchian, keltner, ichimoku } from "./chart-math";
import { addIndicatorHistogram, addIndicatorSeries } from "./chart-engine";

const COLORS: Record<string, string> = {
  EMA20: "#60a5fa", EMA50: "#c084fc", EMA200: "#f97316", SMA20: "#fbbf24",
  WMA20: "#fb923c", VWAP: "#34d399", RSI14: "#e879f9", MACD: "#38bdf8",
  STOCH14: "#f472b6", DONCHIAN: "#64748b", KELTNER: "#0ea5e9",
};

export type IndicatorParameterMap = Readonly<Record<string, Readonly<Record<string, number>>>>;
export type RenderedIndicatorSeries = ISeriesApi<SeriesType>[];

interface IndicatorSeriesSpec {
  kind: "line" | "histogram";
  data: Parameters<typeof addIndicatorSeries>[1];
  color?: string;
  title: string;
  pane: number;
  width?: number;
}

function indicatorSpecs(
  candles: Candle[],
  selected: readonly string[],
  oscillatorPaneIndex: number,
  parameterMap: IndicatorParameterMap,
): IndicatorSeriesSpec[] {
  const specs: IndicatorSeriesSpec[] = [];
  const addLine = (data: IndicatorSeriesSpec["data"], color: string, title: string, pane = 0, width = 1) =>
    specs.push({ kind: "line", data, color, title, pane, width });
  const addHistogram = (data: IndicatorSeriesSpec["data"], title: string, pane = 1) =>
    specs.push({ kind: "histogram", data, title, pane });
  const params = (id: IndicatorId) => normalizeIndicatorParameters(id, parameterMap[id] ?? {});

  for (const rawId of selected) {
    if (!getIndicatorDefinition(rawId)) continue;
    const id = rawId as IndicatorId;
    const p = params(id);
    const period = Math.round(p.period ?? 14);
    switch (id) {
      case "EMA20": addLine(ema(candles, Math.round(p.period ?? 20)), COLORS[id], `EMA ${Math.round(p.period ?? 20)}`, 0, 2); break;
      case "EMA50": addLine(ema(candles, Math.round(p.period ?? 50)), COLORS[id], `EMA ${Math.round(p.period ?? 50)}`, 0, 2); break;
      case "EMA200": addLine(ema(candles, Math.round(p.period ?? 200)), COLORS[id], `EMA ${Math.round(p.period ?? 200)}`, 0, 2); break;
      case "SMA20": addLine(sma(candles, Math.round(p.period ?? 20)), COLORS[id], `SMA ${Math.round(p.period ?? 20)}`, 0, 2); break;
      case "WMA20": addLine(wma(candles, Math.round(p.period ?? 20)), COLORS[id], `WMA ${Math.round(p.period ?? 20)}`, 0, 2); break;
      case "VWAP": addLine(vwap(candles), COLORS[id], "VWAP", 0, 2); break;
      case "ATR14": addLine(atr(candles, period), "#fb7185", `ATR ${period}`, oscillatorPaneIndex); break;
      case "OBV": addLine(obv(candles), "#34d399", "OBV", oscillatorPaneIndex); break;
      case "RSI14": addLine(rsi(candles, period), COLORS[id], `RSI ${period}`, oscillatorPaneIndex, 2); break;
      case "MACD": {
        const value = macd(candles, Math.round(p.fast ?? 12), Math.round(p.slow ?? 26), Math.round(p.signal ?? 9));
        addLine(value.macd, "#38bdf8", "MACD", oscillatorPaneIndex, 2);
        addLine(value.signal, "#f59e0b", "MACD signal", oscillatorPaneIndex, 1);
        addHistogram(value.histogram, "MACD histogram", oscillatorPaneIndex);
        break;
      }
      case "DMI14": {
        const value = dmi(candles, period);
        addLine(value.map(x => ({ time: x.time, value: x.plus })), "#22c55e", "+DI", oscillatorPaneIndex);
        addLine(value.map(x => ({ time: x.time, value: x.minus })), "#ef4444", "-DI", oscillatorPaneIndex);
        addLine(value.map(x => ({ time: x.time, value: x.adx })), "#a78bfa", "ADX", oscillatorPaneIndex, 2);
        break;
      }
      case "STOCH14": addLine(stochastic(candles, period, Math.round(p.smooth ?? 3)), COLORS[id], `Stochastic ${period}`, oscillatorPaneIndex); break;
      case "DONCHIAN20": {
        const value = donchian(candles, Math.round(p.period ?? 20));
        addLine(value.map(x => ({ time: x.time, value: x.upper })), COLORS.DONCHIAN, "Donchian upper");
        addLine(value.map(x => ({ time: x.time, value: x.middle })), "#94a3b8", "Donchian mid");
        addLine(value.map(x => ({ time: x.time, value: x.lower })), COLORS.DONCHIAN, "Donchian lower");
        break;
      }
      case "KELTNER20": {
        const value = keltner(candles, Math.round(p.period ?? 20), Math.round(p.atrPeriod ?? 14), p.multiplier ?? 1.5);
        addLine(value.map(x => ({ time: x.time, value: x.upper })), COLORS.KELTNER, "Keltner upper");
        addLine(value.map(x => ({ time: x.time, value: x.middle })), "#38bdf8", "Keltner mid");
        addLine(value.map(x => ({ time: x.time, value: x.lower })), COLORS.KELTNER, "Keltner lower");
        break;
      }
      case "ICHIMOKU": {
        const value = ichimoku(candles, Math.round(p.conversion ?? 9), Math.round(p.base ?? 26), Math.round(p.span ?? 52));
        addLine(value.map(x => ({ time: x.time, value: x.tenkan })), "#f43f5e", "Ichimoku Tenkan");
        addLine(value.map(x => ({ time: x.time, value: x.kijun })), "#f59e0b", "Ichimoku Kijun");
        addLine(value.map(x => ({ time: x.time, value: x.senkouA })), "#22c55e", "Ichimoku Span A");
        addLine(value.map(x => ({ time: x.time, value: x.senkouB })), "#a855f7", "Ichimoku Span B");
        break;
      }
      case "BB20": {
        const value = bollinger(candles, Math.round(p.period ?? 20), p.stdDev ?? 2);
        addLine(value.map(x => ({ time: x.time, value: x.upper })), "#64748b", "BB upper");
        addLine(value.map(x => ({ time: x.time, value: x.mid })), "#94a3b8", "BB mid");
        addLine(value.map(x => ({ time: x.time, value: x.lower })), "#64748b", "BB lower");
        break;
      }
    }
  }
  return specs;
}

export function renderRegisteredIndicators(
  chart: IChartApi,
  candles: Candle[],
  selected: readonly string[],
  oscillatorPaneIndex = 1,
  parameterMap: IndicatorParameterMap = {},
): RenderedIndicatorSeries {
  return indicatorSpecs(candles, selected, oscillatorPaneIndex, parameterMap).map(spec =>
    spec.kind === "histogram"
      ? addIndicatorHistogram(chart, spec.data, spec.title, spec.pane)
      : addIndicatorSeries(chart, spec.data, spec.color ?? "#94a3b8", spec.title, spec.pane, spec.width ?? 1),
  );
}

/**
 * Refreshes already-created indicator series without recreating the chart graph.
 * The renderer and refresher share the same deterministic spec builder, so series
 * order remains stable as long as the selected indicator set/parameters are stable.
 */
export function refreshRegisteredIndicators(
  rendered: RenderedIndicatorSeries,
  candles: Candle[],
  selected: readonly string[],
  oscillatorPaneIndex = 1,
  parameterMap: IndicatorParameterMap = {},
): void {
  const specs = indicatorSpecs(candles, selected, oscillatorPaneIndex, parameterMap);
  if (rendered.length !== specs.length) return;
  for (let i = 0; i < specs.length; i += 1) {
    const series = rendered[i];
    const data = specs[i].data;
    try {
      series.setData(data as never);
    } catch {
      // Keep the previous valid indicator state if a transient data update is invalid.
    }
  }
}
