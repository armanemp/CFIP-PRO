import type { IChartApi } from "lightweight-charts";
import type { Candle } from "./types";
import { getIndicatorDefinition, type IndicatorId } from "./indicator-registry";
import { atr, ema, bollinger, sma, wma, vwap, obv, rsi, macd, dmi, stochastic, donchian, keltner, ichimoku } from "./chart-math";
import { addIndicatorSeries } from "./chart-engine";
import { TERMINAL_THEME } from "./terminal-theme";

const COLORS: Record<string, string> = {
  EMA20: TERMINAL_THEME.info, EMA50: TERMINAL_THEME.highlight, EMA200: TERMINAL_THEME.warning, SMA20: TERMINAL_THEME.highlight,
  WMA20: TERMINAL_THEME.warning, VWAP: TERMINAL_THEME.bullish, RSI14: TERMINAL_THEME.info, MACD: TERMINAL_THEME.info,
  STOCH14: TERMINAL_THEME.highlight, DONCHIAN: TERMINAL_THEME.textFaint, KELTNER: TERMINAL_THEME.info,
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
      case "ATR14": add(id, atr(candles, 14), TERMINAL_THEME.bearish, "ATR 14", oscillatorPaneIndex); break;
      case "OBV": add(id, obv(candles), TERMINAL_THEME.bullish, "OBV", oscillatorPaneIndex); break;
      case "RSI14": add(id, rsi(candles, 14), COLORS[id], "RSI 14", oscillatorPaneIndex); break;
      case "MACD": {
        const value = macd(candles);
        add(id, value.macd, TERMINAL_THEME.info, "MACD", oscillatorPaneIndex);
        add(id, value.signal, TERMINAL_THEME.warning, "MACD signal", oscillatorPaneIndex);
        break;
      }
      case "DMI14": {
        const value = dmi(candles, 14);
        add(id, value.map(x => ({ time: x.time, value: x.plus })), TERMINAL_THEME.bullish, "DMI +DI", oscillatorPaneIndex);
        add(id, value.map(x => ({ time: x.time, value: x.minus })), TERMINAL_THEME.bearish, "DMI -DI", oscillatorPaneIndex);
        add(id, value.map(x => ({ time: x.time, value: x.adx })), TERMINAL_THEME.highlight, "ADX", oscillatorPaneIndex);
        break;
      }
      case "STOCH14": add(id, stochastic(candles, 14, 3), COLORS[id], "Stochastic 14", oscillatorPaneIndex); break;
      case "DONCHIAN20": {
        const value = donchian(candles, 20);
        add(id, value.map(x => ({ time: x.time, value: x.upper })), COLORS.DONCHIAN, "Donchian upper");
        add(id, value.map(x => ({ time: x.time, value: x.middle })), TERMINAL_THEME.chartText, "Donchian mid");
        add(id, value.map(x => ({ time: x.time, value: x.lower })), COLORS.DONCHIAN, "Donchian lower");
        break;
      }
      case "KELTNER20": {
        const value = keltner(candles, 20, 14, 1.5);
        add(id, value.map(x => ({ time: x.time, value: x.upper })), COLORS.KELTNER, "Keltner upper");
        add(id, value.map(x => ({ time: x.time, value: x.middle })), TERMINAL_THEME.info, "Keltner mid");
        add(id, value.map(x => ({ time: x.time, value: x.lower })), COLORS.KELTNER, "Keltner lower");
        break;
      }
      case "ICHIMOKU": {
        const value = ichimoku(candles);
        add(id, value.map(x => ({ time: x.time, value: x.tenkan })), TERMINAL_THEME.bearish, "Ichimoku Tenkan");
        add(id, value.map(x => ({ time: x.time, value: x.kijun })), TERMINAL_THEME.warning, "Ichimoku Kijun");
        add(id, value.map(x => ({ time: x.time, value: x.senkouA })), TERMINAL_THEME.bullish, "Ichimoku Span A");
        add(id, value.map(x => ({ time: x.time, value: x.senkouB })), TERMINAL_THEME.highlight, "Ichimoku Span B");
        break;
      }
      case "BB20": {
        const value = bollinger(candles);
        add(id, value.map(x => ({ time: x.time, value: x.upper })), TERMINAL_THEME.textFaint, "BB upper");
        add(id, value.map(x => ({ time: x.time, value: x.mid })), TERMINAL_THEME.chartText, "BB mid");
        add(id, value.map(x => ({ time: x.time, value: x.lower })), TERMINAL_THEME.textFaint, "BB lower");
        break;
      }
    }
  }
}
