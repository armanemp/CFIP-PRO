import type { IChartApi } from "lightweight-charts";
import type { Candle } from "./types";
import { getIndicatorDefinition, type IndicatorId } from "./indicator-registry";
import { ema, bollinger, sma, wma, vwap, rsi, macd, dmi, stochastic, donchian, keltner, ichimoku } from "./chart-math";
import { addIndicatorSeries } from "./chart-engine";

const COLORS: Record<string, string> = {
  EMA20: "#60a5fa", EMA50: "#c084fc", EMA200: "#f97316", SMA20: "#fbbf24",
  WMA20: "#fb923c", VWAP: "#34d399", RSI14: "#e879f9", MACD: "#38bdf8",
  STOCH14: "#f472b6", DONCHIAN: "#64748b", KELTNER: "#0ea5e9",
};

export function renderRegisteredIndicators(
  chart: IChartApi,
  candles: Candle[],
  selected: readonly string[],
  oscillatorPaneIndex = 1,
): void {
  const add = (id: string, data: Parameters<typeof addIndicatorSeries>[1], color: string, title: string, pane = 0) =>
    addIndicatorSeries(chart, data, color, title, pane);

  for (const id of selected) {
    if (!getIndicatorDefinition(id)) continue;
    switch (id as IndicatorId) {
      case "EMA20": add(id, ema(candles, 20), COLORS[id], "EMA 20"); break;
      case "EMA50": add(id, ema(candles, 50), COLORS[id], "EMA 50"); break;
      case "EMA200": add(id, ema(candles, 200), COLORS[id], "EMA 200"); break;
      case "SMA20": add(id, sma(candles, 20), COLORS[id], "SMA 20"); break;
      case "WMA20": add(id, wma(candles, 20), COLORS[id], "WMA 20"); break;
      case "VWAP": add(id, vwap(candles), COLORS[id], "VWAP"); break;
      case "RSI14": add(id, rsi(candles, 14), COLORS[id], "RSI 14", oscillatorPaneIndex); break;
      case "MACD": {
        const value = macd(candles);
        add(id, value.macd, "#38bdf8", "MACD", oscillatorPaneIndex);
        add(id, value.signal, "#f59e0b", "MACD signal", oscillatorPaneIndex);
        break;
      }
      case "DMI14": {
        const value = dmi(candles, 14);
        add(id, value.map(x => ({ time: x.time, value: x.plus })), "#22c55e", "DMI +DI", oscillatorPaneIndex);
        add(id, value.map(x => ({ time: x.time, value: x.minus })), "#ef4444", "DMI -DI", oscillatorPaneIndex);
        add(id, value.map(x => ({ time: x.time, value: x.adx })), "#a78bfa", "ADX", oscillatorPaneIndex);
        break;
      }
      case "STOCH14": add(id, stochastic(candles, 14, 3), COLORS[id], "Stochastic 14", oscillatorPaneIndex); break;
      case "DONCHIAN20": {
        const value = donchian(candles, 20);
        add(id, value.map(x => ({ time: x.time, value: x.upper })), COLORS.DONCHIAN, "Donchian upper");
        add(id, value.map(x => ({ time: x.time, value: x.middle })), "#94a3b8", "Donchian mid");
        add(id, value.map(x => ({ time: x.time, value: x.lower })), COLORS.DONCHIAN, "Donchian lower");
        break;
      }
      case "KELTNER20": {
        const value = keltner(candles, 20, 14, 1.5);
        add(id, value.map(x => ({ time: x.time, value: x.upper })), COLORS.KELTNER, "Keltner upper");
        add(id, value.map(x => ({ time: x.time, value: x.middle })), "#38bdf8", "Keltner mid");
        add(id, value.map(x => ({ time: x.time, value: x.lower })), COLORS.KELTNER, "Keltner lower");
        break;
      }
      case "ICHIMOKU": {
        const value = ichimoku(candles);
        add(id, value.map(x => ({ time: x.time, value: x.tenkan })), "#f43f5e", "Ichimoku Tenkan");
        add(id, value.map(x => ({ time: x.time, value: x.kijun })), "#f59e0b", "Ichimoku Kijun");
        add(id, value.map(x => ({ time: x.time, value: x.senkouA })), "#22c55e", "Ichimoku Span A");
        add(id, value.map(x => ({ time: x.time, value: x.senkouB })), "#a855f7", "Ichimoku Span B");
        break;
      }
      case "BB20": {
        const value = bollinger(candles);
        add(id, value.map(x => ({ time: x.time, value: x.upper })), "#64748b", "BB upper");
        add(id, value.map(x => ({ time: x.time, value: x.mid })), "#94a3b8", "BB mid");
        add(id, value.map(x => ({ time: x.time, value: x.lower })), "#64748b", "BB lower");
        break;
      }
    }
  }
}
