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

export type ChartMainSeries = ISeriesApi<SeriesType>;

export function addMainSeries(chart: IChartApi, kind: ChartKind, firstClose: number): ChartMainSeries {
  if (kind === "candles") return chart.addSeries(CandlestickSeries, { upColor: "#22b39b", downColor: "#ef5350", borderUpColor: "#22b39b", borderDownColor: "#ef5350", wickUpColor: "#22b39b", wickDownColor: "#ef5350", priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "bars") return chart.addSeries(BarSeries, { upColor: "#22b39b", downColor: "#ef5350", priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "area") return chart.addSeries(AreaSeries, { lineColor: "#70a7ff", lineWidth: 2, topColor: "rgba(112,167,255,.25)", bottomColor: "rgba(112,167,255,.02)", priceScaleId: "right" }) as ChartMainSeries;
  if (kind === "baseline") return chart.addSeries(BaselineSeries, { baseValue: { type: "price", price: firstClose }, topLineColor: "#22b39b", bottomLineColor: "#ef5350", topFillColor1: "rgba(34,179,155,.18)", topFillColor2: "rgba(34,179,155,.02)", bottomFillColor1: "rgba(239,83,80,.02)", bottomFillColor2: "rgba(239,83,80,.18)", priceScaleId: "right" }) as ChartMainSeries;
  return chart.addSeries(LineSeries, { color: "#70a7ff", lineWidth: 2, priceScaleId: "right" }) as ChartMainSeries;
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

export function addIndicatorSeries(chart: IChartApi, data: LinePoint[], color: string, title: string, paneIndex = 0, lineWidth = 1): ISeriesApi<"Line"> {
  const width = Math.max(1, Math.min(4, Math.round(lineWidth))) as 1 | 2 | 3 | 4;
  const series = chart.addSeries(LineSeries, { color, lineWidth: width, title, priceScaleId: paneIndex === 0 ? "right" : "indicator" }, paneIndex);
  try {
    series.setData(data.length ? data : []);
  } catch {
    series.setData([]);
  }
  return series;
}

export function addIndicatorHistogram(
  chart: IChartApi,
  data: LinePoint[],
  title: string,
  paneIndex = 1,
): ISeriesApi<"Histogram"> {
  const series = chart.addSeries(
    HistogramSeries,
    { priceFormat: { type: "price", precision: 4, minMove: 0.0001 }, title, priceScaleId: "indicator" },
    paneIndex,
  );
  try {
    series.setData(data.length ? data.map(point => ({ time: point.time, value: point.value })) : []);
  } catch {
    series.setData([]);
  }
  return series;
}

export function addVolumeSeries(chart: IChartApi, candles: Candle[], paneIndex = 1): ISeriesApi<"Histogram"> {
  const series = chart.addSeries(HistogramSeries, { priceFormat: { type: "volume" }, priceScaleId: "volume" }, paneIndex);
  series.priceScale().applyOptions({ scaleMargins: { top: 0.84, bottom: 0 } });
  try {
    series.setData(candles.map(({ time, open, close, volume }) => ({ time, value: volume ?? 0, color: close >= open ? "rgba(34,179,155,.32)" : "rgba(239,83,80,.32)" })));
  } catch {
    series.setData([]);
  }
  return series;
}
