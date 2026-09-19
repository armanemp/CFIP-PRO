import type { Candle } from "./types";
import type { LinePoint } from "./chart-series";
import { atr, ema, bollinger, sma, wma, vwap, obv, rsi, macd, dmi, stochastic, donchian, keltner, ichimoku } from "./chart-math";
import { getIndicatorDefinition, defaultIndicatorParameters, type IndicatorId } from "./indicator-registry";
import { TERMINAL_THEME } from "./terminal-theme";

export const INDICATOR_RUNTIME_CONTRACT = "terminal.indicator.runtime.v1";

export interface IndicatorRenderContext {
  candles: Candle[];
  paneIndex: number;
  add: (data: LinePoint[], color: string, title: string, paneIndex?: number) => void;
}

type SeriesSpec = { data: LinePoint[]; color: string; title: string; pane?: number };
type Runtime = (context: IndicatorRenderContext, parameters: Record<string, number>) => SeriesSpec[];

const valueSeries = (values: Array<{ time: Candle["time"]; value: number }>): LinePoint[] =>
  values.filter(point => Number.isFinite(point.value));

const RUNTIME: Partial<Record<IndicatorId, Runtime>> = {
  EMA20: ({ candles }) => [{ data: ema(candles, 20), color: TERMINAL_THEME.info, title: "EMA 20" }],
  EMA50: ({ candles }) => [{ data: ema(candles, 50), color: TERMINAL_THEME.highlight, title: "EMA 50" }],
  EMA200: ({ candles }) => [{ data: ema(candles, 200), color: TERMINAL_THEME.warning, title: "EMA 200" }],
  SMA20: ({ candles }) => [{ data: sma(candles, 20), color: TERMINAL_THEME.highlight, title: "SMA 20" }],
  WMA20: ({ candles }) => [{ data: wma(candles, 20), color: TERMINAL_THEME.warning, title: "WMA 20" }],
  VWAP: ({ candles }) => [{ data: vwap(candles), color: TERMINAL_THEME.bullish, title: "VWAP" }],
  ATR14: ({ candles, paneIndex }) => [{ data: atr(candles, 14), color: TERMINAL_THEME.bearish, title: "ATR 14", pane: paneIndex }],
  OBV: ({ candles, paneIndex }) => [{ data: obv(candles), color: TERMINAL_THEME.bullish, title: "OBV", pane: paneIndex }],
  RSI14: ({ candles, paneIndex }) => [{ data: rsi(candles, 14), color: TERMINAL_THEME.info, title: "RSI 14", pane: paneIndex }],
  MACD: ({ candles, paneIndex }) => {
    const value = macd(candles);
    return [
      { data: value.macd, color: TERMINAL_THEME.info, title: "MACD", pane: paneIndex },
      { data: value.signal, color: TERMINAL_THEME.warning, title: "MACD signal", pane: paneIndex },
    ];
  },
  DMI14: ({ candles, paneIndex }) => {
    const value = dmi(candles, 14);
    return [
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.plus })), color: TERMINAL_THEME.bullish, title: "DMI +DI", pane: paneIndex },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.minus })), color: TERMINAL_THEME.bearish, title: "DMI -DI", pane: paneIndex },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.adx })), color: TERMINAL_THEME.highlight, title: "ADX", pane: paneIndex },
    ];
  },
  STOCH14: ({ candles, paneIndex }) => [{ data: stochastic(candles, 14, 3), color: TERMINAL_THEME.highlight, title: "Stochastic 14", pane: paneIndex }],
  DONCHIAN20: ({ candles }) => {
    const value = donchian(candles, 20);
    return [
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.upper })), color: TERMINAL_THEME.textFaint, title: "Donchian upper" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.middle })), color: TERMINAL_THEME.chartText, title: "Donchian mid" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.lower })), color: TERMINAL_THEME.textFaint, title: "Donchian lower" },
    ];
  },
  KELTNER20: ({ candles }) => {
    const value = keltner(candles, 20, 14, 1.5);
    return [
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.upper })), color: TERMINAL_THEME.info, title: "Keltner upper" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.middle })), color: TERMINAL_THEME.info, title: "Keltner mid" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.lower })), color: TERMINAL_THEME.info, title: "Keltner lower" },
    ];
  },
  ICHIMOKU: ({ candles }) => {
    const value = ichimoku(candles);
    return [
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.tenkan })), color: TERMINAL_THEME.bearish, title: "Ichimoku Tenkan" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.kijun })), color: TERMINAL_THEME.warning, title: "Ichimoku Kijun" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.senkouA })), color: TERMINAL_THEME.bullish, title: "Ichimoku Span A" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.senkouB })), color: TERMINAL_THEME.highlight, title: "Ichimoku Span B" },
    ];
  },
  BB20: ({ candles }) => {
    const value = bollinger(candles);
    return [
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.upper })), color: TERMINAL_THEME.textFaint, title: "BB upper" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.mid })), color: TERMINAL_THEME.chartText, title: "BB mid" },
      { data: valueSeries(value.map(x => ({ time: x.time, value: x.lower })), color: TERMINAL_THEME.textFaint, title: "BB lower" },
    ];
  },
};

export function renderIndicatorRuntime(
  id: string,
  context: IndicatorRenderContext,
  parameters?: Record<string, number>,
): boolean {
  if (!getIndicatorDefinition(id)) return false;
  const runtime = RUNTIME[id as IndicatorId];
  if (!runtime) return false;
  const normalized = parameters ?? defaultIndicatorParameters(id as IndicatorId);
  for (const spec of runtime(context, normalized)) {
    context.add(spec.data, spec.color, spec.title, spec.pane);
  }
  return true;
}
