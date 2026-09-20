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
import type { LinePoint } from "./chart-series";
import { CHART_THEME } from "./chart-theme";

export type ChartMainSeries = ISeriesApi<SeriesType>;

export function addMainSeries(chart: IChartApi, kind: ChartKind, firstClose: number): ChartMainSeries {
  if (kind === "candles") return chart.addSeries(CandlestickSeries, { upColor: CHART_THEME.positive, downColor: CHART_THEME.negative, borderUpColor: CHART_THEME.positive, borderDownColor: CHART_THEME.negative, wickUpColor: CHART_THEME.positive, wickDownColor: CHART_THEME.negative, priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "bars") return chart.addSeries(BarSeries, { upColor: CHART_THEME.positive, downColor: CHART_THEME.negative, priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "area") return chart.addSeries(AreaSeries, { lineColor: CHART_THEME.accent, lineWidth: 2, topColor: CHART_THEME.areaTop, bottomColor: CHART_THEME.areaBottom, priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "baseline") return chart.addSeries(BaselineSeries, { baseValue: { type: "price", price: firstClose }, topLineColor: CHART_THEME.positive, bottomLineColor: CHART_THEME.negative, topFillColor1: CHART_THEME.baselinePositive, topFillColor2: CHART_THEME.baselineNeutral, bottomFillColor1: CHART_THEME.baselineNeutral, bottomFillColor2: CHART_THEME.baselineNegative, priceScaleId: "right" }) as ChartMainSeries;
  return chart.addSeries(LineSeries, { color: CHART_THEME.accent, lineWidth: 2, priceScaleId: "right" }) as ChartMainSeries;
}

function mainPoint(kind: ChartKind, candle: Candle) {
  return kind === "candles" || kind === "bars" ? candle : { time: candle.time, value: candle.close };
}

export function setMainSeriesData(series: ChartMainSeries, kind: ChartKind, candles: Candle[]): void {
  if (!candles.length) return;
  try { series.setData(candles.map(candle => mainPoint(kind, candle)) as never); } catch { /* retain the last valid state */ }
}

export function updateMainSeries(series: ChartMainSeries, kind: ChartKind, candle: Candle): void {
  try { series.update(mainPoint(kind, candle) as never); } catch { /* ignore invalid/out-of-order updates */ }
}

export function addIndicatorSeries(chart: IChartApi, data: LinePoint[], color: string, title: string, paneIndex = 0, lineWidth = 1): ISeriesApi<"Line"> {
  const width = Math.max(1, Math.min(4, Math.round(lineWidth))) as 1 | 2 | 3 | 4;
  const series = chart.addSeries(LineSeries, { color, lineWidth: width, title, priceScaleId: paneIndex === 0 ? "right" : "indicator" }, paneIndex);
  try { series.setData(data.length ? data : []); } catch { /* retain an empty series */ }
  return series;
}

export function addIndicatorHistogram(chart: IChartApi, data: LinePoint[], title: string, paneIndex = 1): ISeriesApi<"Histogram"> {
  const series = chart.addSeries(HistogramSeries, { priceFormat: { type: "price", precision: 4, minMove: 0.0001 }, title, priceScaleId: "indicator" }, paneIndex);
  try { series.setData(data.length ? data.map(point => ({ time: point.time, value: point.value })) : []); } catch { /* retain an empty series */ }
  return series;
}

export function setVolumeSeriesData(series: ISeriesApi<"Histogram">, candles: Candle[]): void {
  try { series.setData(candles.map(({ time, open, close, volume }) => ({ time, value: volume ?? 0, color: close >= open ? CHART_THEME.volumePositive : CHART_THEME.volumeNegative }))); } catch { /* retain the last valid state */ }
}

export function updateVolumeSeries(series: ISeriesApi<"Histogram">, candle: Candle): void {
  try { series.update({ time: candle.time, value: candle.volume ?? 0, color: candle.close >= candle.open ? CHART_THEME.volumePositive : CHART_THEME.volumeNegative }); } catch { /* ignore invalid/out-of-order updates */ }
}

export function addVolumeSeries(chart: IChartApi, candles: Candle[], paneIndex = 1): ISeriesApi<"Histogram"> {
  const series = chart.addSeries(HistogramSeries, { priceFormat: { type: "volume" }, priceScaleId: "volume" }, paneIndex);
  series.priceScale().applyOptions({ scaleMargins: { top: 0.84, bottom: 0 } });
  setVolumeSeriesData(series, candles);
  return series;
}
