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

export function renderRegisteredIndicators(
  chart: IChartApi,
  candles: Candle[],
  selected: readonly string[],
  oscillatorPaneIndex = 1,
  parameterMap: IndicatorParameterMap = {},
): RenderedIndicatorSeries {
  const rendered: RenderedIndicatorSeries = [];
  const add = (data: Parameters<typeof addIndicatorSeries>[1], color: string, title: string, pane = 0, width = 1) => {
    const series = addIndicatorSeries(chart, data, color, title, pane, width);
    rendered.push(series);
  };
  const params = (id: IndicatorId) => normalizeIndicatorParameters(id, parameterMap[id] ?? {});

  for (const rawId of selected) {
    if (!getIndicatorDefinition(rawId)) continue;
    const id = rawId as IndicatorId;
    const p = params(id);
    const period = Math.round(p.period ?? 14);
    switch (id) {
      case "EMA20": add(ema(candles, Math.round(p.period ?? 20)), COLORS[id], `EMA ${Math.round(p.period ?? 20)}`, 0, 2); break;
      case "EMA50": add(ema(candles, Math.round(p.period ?? 50)), COLORS[id], `EMA ${Math.round(p.period ?? 50)}`, 0, 2); break;
      case "EMA200": add(ema(candles, Math.round(p.period ?? 200)), COLORS[id], `EMA ${Math.round(p.period ?? 200)}`, 0, 2); break;
      case "SMA20": add(sma(candles, Math.round(p.period ?? 20)), COLORS[id], `SMA ${Math.round(p.period ?? 20)}`, 0, 2); break;
      case "WMA20": add(wma(candles, Math.round(p.period ?? 20)), COLORS[id], `WMA ${Math.round(p.period ?? 20)}`, 0, 2); break;
      case "VWAP": add(vwap(candles), COLORS[id], "VWAP", 0, 2); break;
      case "ATR14": add(atr(candles, period), "#fb7185", `ATR ${period}`, oscillatorPaneIndex); break;
      case "OBV": add(obv(candles), "#34d399", "OBV", oscillatorPaneIndex); break;
      case "RSI14": add(rsi(candles, period), COLORS[id], `RSI ${period}`, oscillatorPaneIndex, 2); break;
      case "MACD": {
        const value = macd(candles, Math.round(p.fast ?? 12), Math.round(p.slow ?? 26), Math.round(p.signal ?? 9));
        add(value.macd, "#38bdf8", "MACD", oscillatorPaneIndex, 2);
        add(value.signal, "#f59e0b", "MACD signal", oscillatorPaneIndex, 1);
        rendered.push(addIndicatorHistogram(chart, value.histogram, "MACD histogram", oscillatorPaneIndex));
        break;
      }
      case "DMI14": {
        const value = dmi(candles, period);
        add(value.map(x => ({ time: x.time, value: x.plus })), "#22c55e", "+DI", oscillatorPaneIndex);
        add(value.map(x => ({ time: x.time, value: x.minus })), "#ef4444", "-DI", oscillatorPaneIndex);
        add(value.map(x => ({ time: x.time, value: x.adx })), "#a78bfa", "ADX", oscillatorPaneIndex, 2);
        break;
      }
      case "STOCH14": add(stochastic(candles, period, Math.round(p.smooth ?? 3)), COLORS[id], `Stochastic ${period}`, oscillatorPaneIndex); break;
      case "DONCHIAN20": {
        const value = donchian(candles, Math.round(p.period ?? 20));
        add(value.map(x => ({ time: x.time, value: x.upper })), COLORS.DONCHIAN, "Donchian upper");
        add(value.map(x => ({ time: x.time, value: x.middle })), "#94a3b8", "Donchian mid");
        add(value.map(x => ({ time: x.time, value: x.lower })), COLORS.DONCHIAN, "Donchian lower");
        break;
      }
      case "KELTNER20": {
        const value = keltner(candles, Math.round(p.period ?? 20), Math.round(p.atrPeriod ?? 14), p.multiplier ?? 1.5);
        add(value.map(x => ({ time: x.time, value: x.upper })), COLORS.KELTNER, "Keltner upper");
        add(value.map(x => ({ time: x.time, value: x.middle })), "#38bdf8", "Keltner mid");
        add(value.map(x => ({ time: x.time, value: x.lower })), COLORS.KELTNER, "Keltner lower");
        break;
      }
      case "ICHIMOKU": {
        const value = ichimoku(candles, Math.round(p.conversion ?? 9), Math.round(p.base ?? 26), Math.round(p.span ?? 52));
        add(value.map(x => ({ time: x.time, value: x.tenkan })), "#f43f5e", "Ichimoku Tenkan");
        add(value.map(x => ({ time: x.time, value: x.kijun })), "#f59e0b", "Ichimoku Kijun");
        add(value.map(x => ({ time: x.time, value: x.senkouA })), "#22c55e", "Ichimoku Span A");
        add(value.map(x => ({ time: x.time, value: x.senkouB })), "#a855f7", "Ichimoku Span B");
        break;
      }
      case "BB20": {
        const value = bollinger(candles, Math.round(p.period ?? 20), p.stdDev ?? 2);
        add(value.map(x => ({ time: x.time, value: x.upper })), "#64748b", "BB upper");
        add(value.map(x => ({ time: x.time, value: x.mid })), "#94a3b8", "BB mid");
        add(value.map(x => ({ time: x.time, value: x.lower })), "#64748b", "BB lower");
        break;
      }
    }
  }
  return rendered;
}
