import {
  AreaSeries,
  BarSeries,
  BaselineSeries,
  CandlestickSeries,
  HistogramSeries,
  LineSeries,
  type IChartApi,
  type ISeriesApi,
  type SeriesType,
} from "lightweight-charts";
import type { Candle, ChartKind } from "./types";
import { TERMINAL_THEME } from "./terminal-theme";
import type { LinePoint } from "./chart-series";

export type ChartMainSeries = ISeriesApi<SeriesType>;

export function addMainSeries(chart: IChartApi, kind: ChartKind, firstClose: number): ChartMainSeries {
  if (kind === "candles") return chart.addSeries(CandlestickSeries, { upColor: TERMINAL_THEME.bullish, downColor: TERMINAL_THEME.bearish, borderUpColor: TERMINAL_THEME.bullish, borderDownColor: TERMINAL_THEME.bearish, wickUpColor: TERMINAL_THEME.bullish, wickDownColor: TERMINAL_THEME.bearish, priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "bars") return chart.addSeries(BarSeries, { upColor: TERMINAL_THEME.bullish, downColor: TERMINAL_THEME.bearish, priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "area") return chart.addSeries(AreaSeries, { lineColor: TERMINAL_THEME.volumeUp, lineWidth: 2, topColor: `${TERMINAL_THEME.volumeUp}40`, bottomColor: `${TERMINAL_THEME.volumeUp}05`, priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "baseline") return chart.addSeries(BaselineSeries, { baseValue: { type: "price", price: firstClose }, topLineColor: TERMINAL_THEME.bullish, bottomLineColor: TERMINAL_THEME.bearish, topFillColor1: `${TERMINAL_THEME.bullish}2e`, topFillColor2: `${TERMINAL_THEME.bullish}05`, bottomFillColor1: `${TERMINAL_THEME.bearish}05`, bottomFillColor2: `${TERMINAL_THEME.bearish}2e`, priceScaleId: "right" }) as ChartMainSeries;
  return chart.addSeries(LineSeries, { color: TERMINAL_THEME.volumeUp, lineWidth: 2, priceScaleId: "right" }) as ChartMainSeries;
}

export function setMainSeriesData(series: ChartMainSeries, kind: ChartKind, candles: Candle[]): void {
  if (!candles.length) return;
  try {
    if (kind === "candles" || kind === "bars") series.setData(candles);
    else series.setData(candles.map(({ time, close }) => ({ time, value: close })));
  } catch {
    series.setData([]);
  }
}

export function addIndicatorSeries(chart: IChartApi, data: LinePoint[], color: string, title: string, paneIndex = 0): ISeriesApi<"Line"> {
  const series = chart.addSeries(LineSeries, { color, lineWidth: 1, title, priceScaleId: paneIndex === 0 ? "right" : "indicator" }, paneIndex);
  try {
    series.setData(data.length ? data : []);
  } catch {
    series.setData([]);
  }
  return series;
}

export function addVolumeSeries(chart: IChartApi, candles: Candle[], paneIndex = 1): ISeriesApi<"Histogram"> {
  const series = chart.addSeries(HistogramSeries, { priceFormat: { type: "volume" }, priceScaleId: "volume" }, paneIndex);
  series.priceScale().applyOptions({ scaleMargins: { top: 0.84, bottom: 0 } });
  try {
    series.setData(candles.map(({ time, open, close, volume }) => ({ time, value: volume ?? 0, color: close >= open ? `${TERMINAL_THEME.bullish}52` : `${TERMINAL_THEME.bearish}52` })));
  } catch {
    series.setData([]);
  }
  return series;
}
