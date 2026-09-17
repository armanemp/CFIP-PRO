"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import {
  AreaSeries,
  BarSeries,
  CandlestickSeries,
  ColorType,
  CrosshairMode,
  HistogramSeries,
  LineSeries,
  createChart,
  type IChartApi,
  type ISeriesApi,
  type SeriesType,
  type UTCTimestamp,
} from "lightweight-charts";

import type { MarketObservation } from "@/lib/api";

type ChartMode = "candles" | "line" | "area" | "bars";
type Timeframe = "1m" | "5m" | "15m" | "1H" | "4H" | "1D";
type Indicator = "sma20" | "ema20" | "ema50" | "bb20" | "vwap";
type DrawingTool = "cursor" | "horizontal" | "vertical" | "trendline";

interface Candle {
  time: UTCTimestamp;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}
interface Zone {
  start: UTCTimestamp;
  end: UTCTimestamp;
  top: number;
  bottom: number;
  kind: "bullish" | "bearish";
  label: string;
}
interface StructurePoint {
  time: UTCTimestamp;
  price: number;
  label: string;
}
interface Drawing {
  tool: Exclude<DrawingTool, "cursor">;
  p1: { time: UTCTimestamp; price: number };
  p2: { time: UTCTimestamp; price: number };
}

const TIMEFRAME_SECONDS: Record<Timeframe, number> = {
  "1m": 60,
  "5m": 300,
  "15m": 900,
  "1H": 3600,
  "4H": 14400,
  "1D": 86400,
};
const TIMEFRAMES: Timeframe[] = ["1m", "5m", "15m", "1H", "4H", "1D"];
const INDICATORS: { id: Indicator; label: string }[] = [
  { id: "sma20", label: "SMA 20" },
  { id: "ema20", label: "EMA 20" },
  { id: "ema50", label: "EMA 50" },
  { id: "bb20", label: "Bollinger 20" },
  { id: "vwap", label: "VWAP" },
];

function aggregateObservations(observations: MarketObservation[], timeframe: Timeframe): Candle[] {
  const seconds = TIMEFRAME_SECONDS[timeframe];
  const sorted = observations
    .map((observation) => ({ observation, epoch: Math.floor(new Date(observation.observed_at).getTime() / 1000) }))
    .filter(({ observation, epoch }) => Number.isFinite(epoch) && (observation.last ?? observation.bid ?? observation.ask) !== null)
    .sort((a, b) => a.epoch - b.epoch);
  const buckets = new Map<number, Candle>();

  for (const { observation, epoch } of sorted) {
    const rawValue = observation.last ?? observation.bid ?? observation.ask;
    if (rawValue === null) continue;
    const price = Number(rawValue);
    if (!Number.isFinite(price)) continue;
    const bucket = Math.floor(epoch / seconds) * seconds;
    const rawVolume = observation.volume === null ? 0 : Number(observation.volume);
    const volume = Number.isFinite(rawVolume) && rawVolume >= 0 ? rawVolume : 0;
    const existing = buckets.get(bucket);
    if (!existing) {
      buckets.set(bucket, {
        time: bucket as UTCTimestamp,
        open: price,
        high: price,
        low: price,
        close: price,
        volume,
      });
      continue;
    }
    existing.high = Math.max(existing.high, price);
    existing.low = Math.min(existing.low, price);
    existing.close = price;
    existing.volume += volume;
  }
  return [...buckets.values()].sort((a, b) => Number(a.time) - Number(b.time));
}

function sma(candles: Candle[], period: number) {
  return candles.flatMap((candle, index) => {
    if (index + 1 < period) return [];
    const window = candles.slice(index + 1 - period, index + 1);
    return [{ time: candle.time, value: window.reduce((sum, item) => sum + item.close, 0) / period }];
  });
}

function ema(candles: Candle[], period: number) {
  if (candles.length < period) return [] as { time: UTCTimestamp; value: number }[];
  const result: { time: UTCTimestamp; value: number }[] = [];
  let value = candles.slice(0, period).reduce((sum, candle) => sum + candle.close, 0) / period;
  result.push({ time: candles[period - 1].time, value });
  const multiplier = 2 / (period + 1);
  for (let index = period; index < candles.length; index += 1) {
    value = (candles[index].close - value) * multiplier + value;
    result.push({ time: candles[index].time, value });
  }
  return result;
}

function bollinger(candles: Candle[], period = 20, deviation = 2) {
  return candles.flatMap((candle, index) => {
    if (index + 1 < period) return [];
    const window = candles.slice(index + 1 - period, index + 1).map((item) => item.close);
    const mean = window.reduce((sum, value) => sum + value, 0) / period;
    const variance = window.reduce((sum, value) => sum + (value - mean) ** 2, 0) / period;
    const width = Math.sqrt(variance) * deviation;
    return [{ time: candle.time, upper: mean + width, middle: mean, lower: mean - width }];
  });
}

function vwap(candles: Candle[]) {
  let cumulativeVolume = 0;
  let cumulativeValue = 0;
  return candles.map((candle) => {
    const volume = candle.volume > 0 ? candle.volume : 1;
    cumulativeVolume += volume;
    cumulativeValue += ((candle.high + candle.low + candle.close) / 3) * volume;
    return { time: candle.time, value: cumulativeValue / cumulativeVolume };
  });
}

function detectFvg(candles: Candle[]): Zone[] {
  const zones: Zone[] = [];
  const lastTime = candles.at(-1)?.time;
  if (lastTime === undefined) return zones;
  for (let i = 2; i < candles.length; i += 1) {
    const left = candles[i - 2];
    const middle = candles[i - 1];
    const right = candles[i];
    if (left.high < right.low && middle.high >= left.high && middle.low <= right.low) {
      const bottom = left.high;
      const top = right.low;
      const mitigated = candles.slice(i + 1).some((candle) => candle.low <= bottom);
      if (!mitigated) zones.push({ start: middle.time, end: lastTime, top, bottom, kind: "bullish", label: "FVG" });
    } else if (left.low > right.high && middle.high >= left.low && middle.low <= right.low) {
      const bottom = right.high;
      const top = left.low;
      const mitigated = candles.slice(i + 1).some((candle) => candle.high >= top);
      if (!mitigated) zones.push({ start: middle.time, end: lastTime, top, bottom, kind: "bearish", label: "FVG" });
    }
  }
  return zones.slice(-12);
}

function detectOrderBlocks(candles: Candle[]): Zone[] {
  const zones: Zone[] = [];
  const lastTime = candles.at(-1)?.time;
  if (lastTime === undefined) return zones;
  for (let i = 2; i < candles.length; i += 1) {
    const previous = candles[i - 1];
    const current = candles[i];
    const range = Math.max(current.high - current.low, Number.EPSILON);
    const body = Math.abs(current.close - current.open);
    if (body / range < 0.6) continue;
    const bullish = current.close > current.open && previous.close < previous.open;
    const bearish = current.close < current.open && previous.close > previous.open;
    if (!bullish && !bearish) continue;
    const mitigated = bullish
      ? candles.slice(i + 1).some((candle) => candle.low <= previous.low)
      : candles.slice(i + 1).some((candle) => candle.high >= previous.high);
    if (!mitigated) {
      zones.push({
        start: previous.time,
        end: lastTime,
        top: previous.high,
        bottom: previous.low,
        kind: bullish ? "bullish" : "bearish",
        label: "OB",
      });
    }
  }
  return zones.slice(-8);
}

function detectStructure(candles: Candle[]): StructurePoint[] {
  const points: StructurePoint[] = [];
  for (let i = 2; i < candles.length - 2; i += 1) {
    const previous = candles[i - 1];
    const current = candles[i];
    const next = candles[i + 1];
    if (current.high > previous.high && current.high >= next.high) {
      points.push({ time: current.time, price: current.high, label: "HH" });
    } else if (current.low < previous.low && current.low <= next.low) {
      points.push({ time: current.time, price: current.low, label: "LL" });
    }
  }
  return points.slice(-20);
}

function calculateRsi(candles: Candle[], period = 14) {
  if (candles.length <= period) return [] as { time: UTCTimestamp; value: number }[];
  let gain = 0;
  let loss = 0;
  for (let i = 1; i <= period; i += 1) {
    const delta = candles[i].close - candles[i - 1].close;
    gain += Math.max(delta, 0);
    loss += Math.max(-delta, 0);
  }
  let averageGain = gain / period;
  let averageLoss = loss / period;
  const values: { time: UTCTimestamp; value: number }[] = [
    { time: candles[period].time, value: averageLoss === 0 ? 100 : 100 - 100 / (1 + averageGain / averageLoss) },
  ];
  for (let i = period + 1; i < candles.length; i += 1) {
    const delta = candles[i].close - candles[i - 1].close;
    const currentGain = Math.max(delta, 0);
    const currentLoss = Math.max(-delta, 0);
    averageGain = (averageGain * (period - 1) + currentGain) / period;
    averageLoss = (averageLoss * (period - 1) + currentLoss) / period;
    const value = averageLoss === 0 ? 100 : 100 - 100 / (1 + averageGain / averageLoss);
    values.push({ time: candles[i].time, value });
  }
  return values;
}

const chartOptions = {
  autoSize: true,
  layout: { background: { type: ColorType.Solid, color: "#070a0f" }, textColor: "#8f9aaa", attributionLogo: false },
  grid: { vertLines: { color: "#111823" }, horzLines: { color: "#111823" } },
  rightPriceScale: { borderColor: "#1d2734", autoScale: true },
  timeScale: { borderColor: "#1d2734", timeVisible: true, secondsVisible: false, rightOffset: 8 },
  crosshair: { mode: CrosshairMode.Normal, vertLine: { labelBackgroundColor: "#26364a" }, horzLine: { labelBackgroundColor: "#26364a" } },
  handleScroll: { mouseWheel: true, pressedMouseMove: true, horzTouchDrag: true, vertTouchDrag: true },
  handleScale: { mouseWheel: true, pinch: true, axisPressedMouseMove: true },
} as const;

interface MarketChartProps { observations: MarketObservation[]; }

export function MarketChart({ observations }: MarketChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<SeriesType>[]>([]);
  const oscillatorRef = useRef<IChartApi | null>(null);
  const [chart, setChart] = useState<IChartApi | null>(null);
  const [timeframe, setTimeframe] = useState<Timeframe>("15m");
  const [chartMode, setChartMode] = useState<ChartMode>("candles");
  const [showVolume, setShowVolume] = useState(true);
  const [showFvg, setShowFvg] = useState(false);
  const [showOrderBlocks, setShowOrderBlocks] = useState(false);
  const [showStructure, setShowStructure] = useState(false);
  const [showOscillator, setShowOscillator] = useState(false);
  const [activeIndicators, setActiveIndicators] = useState<Indicator[]>(["ema20"]);
  const [drawingTool, setDrawingTool] = useState<DrawingTool>("cursor");
  const [drawings, setDrawings] = useState<Drawing[]>([]);
  const [drawingStart, setDrawingStart] = useState<{ time: UTCTimestamp; price: number } | null>(null);

  const candles = useMemo(() => aggregateObservations(observations, timeframe), [observations, timeframe]);
  const fvgZones = useMemo(() => detectFvg(candles), [candles]);
  const orderBlocks = useMemo(() => detectOrderBlocks(candles), [candles]);
  const structure = useMemo(() => detectStructure(candles), [candles]);
  const latest = candles.at(-1);
  const previous = candles.at(-2);
  const change = latest && previous ? latest.close - previous.close : 0;
  const changePct = previous && previous.close !== 0 ? (change / previous.close) * 100 : 0;

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;
    const nextChart = createChart(container, chartOptions);
    chartRef.current = nextChart;
    setChart(nextChart);
    const resizeObserver = new ResizeObserver(() => nextChart.resize(container.clientWidth, container.clientHeight));
    resizeObserver.observe(container);
    return () => {
      resizeObserver.disconnect();
      seriesRef.current = [];
      nextChart.remove();
      if (chartRef.current === nextChart) chartRef.current = null;
      setChart((current) => current === nextChart ? null : current);
    };
  }, []);

  useEffect(() => {
    const nextChart = chartRef.current;
    if (!nextChart) return;
    nextChart.applyOptions(chartOptions);
    for (const existingSeries of seriesRef.current) nextChart.removeSeries(existingSeries);
    seriesRef.current = [];
    const addSeries = <T extends SeriesType>(nextSeries: ISeriesApi<T>): ISeriesApi<T> => {
      seriesRef.current.push(nextSeries as ISeriesApi<SeriesType>);
      return nextSeries;
    };
    let series: ISeriesApi<SeriesType>;
    if (chartMode === "candles") {
      series = addSeries(nextChart.addSeries(CandlestickSeries, { upColor: "#36c98f", downColor: "#f05d5e", borderVisible: false, wickUpColor: "#36c98f", wickDownColor: "#f05d5e" }));
      series.setData(candles);
    } else if (chartMode === "bars") {
      series = addSeries(nextChart.addSeries(BarSeries, { upColor: "#36c98f", downColor: "#f05d5e" }));
      series.setData(candles);
    } else if (chartMode === "area") {
      series = addSeries(nextChart.addSeries(AreaSeries, { lineWidth: 2, lineColor: "#4ca6ff", topColor: "rgba(76,166,255,0.20)", bottomColor: "rgba(76,166,255,0.01)" }));
      series.setData(candles.map((candle) => ({ time: candle.time, value: candle.close })));
    } else {
      series = addSeries(nextChart.addSeries(LineSeries, { lineWidth: 2, color: "#4ca6ff" }));
      series.setData(candles.map((candle) => ({ time: candle.time, value: candle.close })));
    }

    if (showVolume && candles.length) {
      const volume = addSeries(nextChart.addSeries(HistogramSeries, { priceFormat: { type: "volume" }, priceScaleId: "volume" }));
      volume.priceScale().applyOptions({ scaleMargins: { top: 0.82, bottom: 0 } });
      volume.setData(candles.map((candle) => ({ time: candle.time, value: candle.volume, color: candle.close >= candle.open ? "rgba(54,201,143,0.38)" : "rgba(240,93,94,0.38)" })));
    }

    const indicators = new Map<Indicator, { time: UTCTimestamp; value: number }[]>();
    if (activeIndicators.includes("sma20")) indicators.set("sma20", sma(candles, 20));
    if (activeIndicators.includes("ema20")) indicators.set("ema20", ema(candles, 20));
    if (activeIndicators.includes("ema50")) indicators.set("ema50", ema(candles, 50));
    if (activeIndicators.includes("vwap")) indicators.set("vwap", vwap(candles));
    const indicatorColors: Record<Indicator, string> = { sma20: "#d8a84e", ema20: "#4ca6ff", ema50: "#b77cff", bb20: "#8f9aaa", vwap: "#f3b562" };
    for (const indicator of ["sma20", "ema20", "ema50", "vwap"] as Indicator[]) {
      const data = indicators.get(indicator);
      if (!data) continue;
      const line = addSeries(nextChart.addSeries(LineSeries, { lineWidth: 1, color: indicatorColors[indicator], priceLineVisible: false, lastValueVisible: false }));
      line.setData(data);
    }
    if (activeIndicators.includes("bb20")) {
      const bands = bollinger(candles);
      for (const key of ["upper", "middle", "lower"] as const) {
        const line = addSeries(nextChart.addSeries(LineSeries, { lineWidth: 1, color: key === "middle" ? "#8f9aaa" : "rgba(143,154,170,0.65)", lineStyle: key === "middle" ? 0 : 2, priceLineVisible: false, lastValueVisible: false }));
        line.setData(bands.map((band) => ({ time: band.time, value: band[key] })));
      }
    }
    if (candles.length) nextChart.timeScale().fitContent();
  }, [candles, chartMode, showVolume, activeIndicators]);

  useEffect(() => {
    if (!showOscillator || candles.length <= 14) {
      oscillatorRef.current?.remove();
      oscillatorRef.current = null;
      return;
    }
    const host = document.getElementById("cfip-oscillator");
    if (!host) return;
    oscillatorRef.current?.remove();
    const oscillator = createChart(host, {
      autoSize: true,
      layout: { background: { type: ColorType.Solid, color: "#090d13" }, textColor: "#7d8998" },
      grid: { vertLines: { color: "#111823" }, horzLines: { color: "#111823" } },
      rightPriceScale: { borderColor: "#1d2734", autoScale: false, scaleMargins: { top: 0.08, bottom: 0.08 } },
      timeScale: { borderColor: "#1d2734", visible: false },
      crosshair: { mode: CrosshairMode.Normal },
    });
    oscillatorRef.current = oscillator;
    const rsi = oscillator.addSeries(LineSeries, { color: "#d8a84e", lineWidth: 1, priceLineVisible: false });
    rsi.setData(calculateRsi(candles));
    oscillator.priceScale("right").applyOptions({ autoScale: false, minValue: 0, maxValue: 100 });
    oscillator.timeScale().fitContent();
    return () => {
      oscillator.remove();
      if (oscillatorRef.current === oscillator) oscillatorRef.current = null;
    };
  }, [candles, showOscillator]);

  const toggleIndicator = (indicator: Indicator) => setActiveIndicators((current) => current.includes(indicator) ? current.filter((item) => item !== indicator) : [...current, indicator]);
  const resetView = () => chartRef.current?.timeScale().fitContent();
  const clearDrawings = () => { setDrawings([]); setDrawingStart(null); };

  return (
    <div className="relative flex h-full min-h-0 flex-col bg-[var(--terminal-bg)] text-[var(--terminal-text)]">
      <div className="flex shrink-0 items-center gap-1 overflow-x-auto border-b border-[var(--terminal-border)] bg-[var(--terminal-panel)] px-2 py-1">
        <div className="mr-2 flex items-center gap-1 border-r border-[var(--terminal-border)] pr-2">
          {TIMEFRAMES.map((item) => <button key={item} onClick={() => setTimeframe(item)} className={`rounded px-2 py-1 text-xs ${timeframe === item ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)] hover:text-white"}`}>{item}</button>)}
        </div>
        <select value={chartMode} onChange={(event) => setChartMode(event.target.value as ChartMode)} className="rounded border border-[var(--terminal-border)] bg-[#111823] px-2 py-1 text-xs"><option value="candles">Candles</option><option value="bars">Bars</option><option value="line">Line</option><option value="area">Area</option></select>
        <button onClick={() => setShowVolume((value) => !value)} className={`rounded px-2 py-1 text-xs ${showVolume ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)]"}`}>Volume</button>
        {INDICATORS.map((indicator) => <button key={indicator.id} onClick={() => toggleIndicator(indicator.id)} className={`rounded px-2 py-1 text-xs ${activeIndicators.includes(indicator.id) ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)]"}`}>{indicator.label}</button>)}
        <button onClick={() => setShowOscillator((value) => !value)} className={`rounded px-2 py-1 text-xs ${showOscillator ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)]"}`}>RSI</button>
        <div className="ml-auto flex items-center gap-1"><button onClick={resetView} className="rounded px-2 py-1 text-xs text-[var(--terminal-muted)] hover:text-white">Fit</button><button onClick={clearDrawings} className="rounded px-2 py-1 text-xs text-[var(--terminal-muted)] hover:text-white">Clear drawings</button></div>
      </div>
      <div className="flex shrink-0 items-center gap-4 border-b border-[var(--terminal-border)] bg-[#090d13] px-3 py-2 text-xs">
        <strong className="text-sm">EUR/USD</strong><span className="text-[var(--terminal-muted)]">{timeframe}</span>
        {latest && <><span>O {latest.open.toFixed(5)}</span><span>H {latest.high.toFixed(5)}</span><span>L {latest.low.toFixed(5)}</span><span>C {latest.close.toFixed(5)}</span><span className={change >= 0 ? "text-emerald-400" : "text-red-400"}>{change >= 0 ? "+" : ""}{change.toFixed(5)} ({changePct.toFixed(2)}%)</span></>}
        <span className="ml-auto text-[var(--terminal-muted)]">{candles.length} candles · {observations.length} observations</span>
      </div>
      <div className="relative min-h-0 flex-1">
        <div ref={containerRef} className="absolute inset-0" aria-label="CFIP full market chart" />
        <div className="pointer-events-none absolute inset-0">
          {showFvg && fvgZones.map((zone, index) => <ZoneBadge key={`fvg-${index}`} zone={zone} chart={chart} />)}
          {showOrderBlocks && orderBlocks.map((zone, index) => <ZoneBadge key={`ob-${index}`} zone={zone} chart={chart} />)}
          {showStructure && structure.map((point, index) => <StructureBadge key={`structure-${index}`} point={point} chart={chart} />)}
          <DrawingLayer tool={drawingTool} drawings={drawings} drawingStart={drawingStart} chart={chart} setDrawingStart={setDrawingStart} setDrawings={setDrawings} />
        </div>
        <div className="absolute left-2 top-2 z-30 flex flex-col gap-1 rounded border border-[var(--terminal-border)] bg-[#0d121a]/90 p-1 backdrop-blur">
          {(["cursor", "horizontal", "vertical", "trendline"] as DrawingTool[]).map((tool) => <button key={tool} onClick={() => setDrawingTool(tool)} className={`rounded px-2 py-1 text-left text-[10px] ${drawingTool === tool ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)] hover:text-white"}`}>{tool}</button>)}
          <button onClick={() => setShowFvg((value) => !value)} className={`rounded px-2 py-1 text-left text-[10px] ${showFvg ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)]"}`}>FVG</button>
          <button onClick={() => setShowOrderBlocks((value) => !value)} className={`rounded px-2 py-1 text-left text-[10px] ${showOrderBlocks ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)]"}`}>Order Block</button>
          <button onClick={() => setShowStructure((value) => !value)} className={`rounded px-2 py-1 text-left text-[10px] ${showStructure ? "bg-[#26364a] text-white" : "text-[var(--terminal-muted)]"}`}>Structure</button>
        </div>
      </div>
      {showOscillator && <div id="cfip-oscillator" className="h-28 shrink-0 border-t border-[var(--terminal-border)]" aria-label="RSI oscillator" />}
      <div className="flex shrink-0 items-center justify-between border-t border-[var(--terminal-border)] bg-[var(--terminal-panel)] px-3 py-1 text-[10px] text-[var(--terminal-muted)]"><span>Crosshair · wheel zoom · drag pan · axis scale · drawings · FVG · Order Blocks · structure</span><span>Source: normalized market observations</span></div>
    </div>
  );
}

function ZoneBadge({ zone, chart }: { zone: Zone; chart: IChartApi | null }) {
  const [style, setStyle] = useState<{ left: number; top: number; width: number; height: number } | null>(null);
  useEffect(() => {
    if (!chart) return;
    const update = () => {
      const x1 = chart.timeScale().timeToCoordinate(zone.start);
      const x2 = chart.timeScale().timeToCoordinate(zone.end);
      const y1 = chart.priceScale("right").priceToCoordinate(zone.top);
      const y2 = chart.priceScale("right").priceToCoordinate(zone.bottom);
      if (x1 === null || x2 === null || y1 === null || y2 === null) return;
      setStyle({ left: Math.min(x1, x2), top: Math.min(y1, y2), width: Math.max(2, Math.abs(x2 - x1)), height: Math.max(2, Math.abs(y2 - y1)) });
    };
    update();
    chart.timeScale().subscribeVisibleTimeRangeChange(update);
    return () => chart.timeScale().unsubscribeVisibleTimeRangeChange(update);
  }, [chart, zone]);
  if (!style) return null;
  const className = zone.kind === "bullish" ? "border border-emerald-400/40 bg-emerald-400/10" : "border border-red-400/40 bg-red-400/10";
  return <div className={`absolute ${className}`} style={{ left: style.left, top: style.top, width: style.width, height: style.height }} aria-label={`${zone.kind} ${zone.label}`} />;
}

function StructureBadge({ point, chart }: { point: StructurePoint; chart: IChartApi | null }) {
  const [position, setPosition] = useState<{ left: number; top: number } | null>(null);
  useEffect(() => {
    if (!chart) return;
    const update = () => {
      const x = chart.timeScale().timeToCoordinate(point.time);
      const y = chart.priceScale("right").priceToCoordinate(point.price);
      if (x !== null && y !== null) setPosition({ left: x, top: y });
    };
    update();
    chart.timeScale().subscribeVisibleTimeRangeChange(update);
    return () => chart.timeScale().unsubscribeVisibleTimeRangeChange(update);
  }, [chart, point]);
  if (!position) return null;
  return <span className="absolute rounded bg-[#0d121a]/85 px-1 text-[9px] text-[#d8a84e]" style={{ left: position.left + 3, top: position.top - 10 }}>{point.label}</span>;
}

function DrawingLayer({ tool, drawings, drawingStart, chart, setDrawingStart, setDrawings }: {
  tool: DrawingTool;
  drawings: Drawing[];
  drawingStart: { time: UTCTimestamp; price: number } | null;
  chart: IChartApi | null;
  setDrawingStart: (value: { time: UTCTimestamp; price: number } | null) => void;
  setDrawings: (value: Drawing[] | ((current: Drawing[]) => Drawing[])) => void;
}) {
  const [version, setVersion] = useState(0);
  useEffect(() => {
    if (!chart) return;
    const update = () => setVersion((value) => value + 1);
    chart.timeScale().subscribeVisibleTimeRangeChange(update);
    return () => chart.timeScale().unsubscribeVisibleTimeRangeChange(update);
  }, [chart]);

  const toPixel = (point: { time: UTCTimestamp; price: number }) => {
    if (!chart) return null;
    const x = chart.timeScale().timeToCoordinate(point.time);
    const y = chart.priceScale("right").priceToCoordinate(point.price);
    return x === null || y === null ? null : { x, y };
  };
  const pixelToPoint = (event: React.PointerEvent<SVGSVGElement>) => {
    if (!chart) return null;
    const rect = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    const time = chart.timeScale().coordinateToTime(x);
    const price = chart.priceScale("right").coordinateToPrice(y);
    if (time === null || price === null || !Number.isFinite(price)) return null;
    return { time: time as UTCTimestamp, price };
  };

  void version;
  const startPixel = drawingStart ? toPixel(drawingStart) : null;
  return (
    <svg
      className={`absolute inset-0 z-20 h-full w-full ${tool === "cursor" ? "pointer-events-none" : "pointer-events-auto"}`}
      onPointerDown={(event) => {
        if (tool === "cursor") return;
        const point = pixelToPoint(event);
        if (!point) return;
        if (!drawingStart) {
          setDrawingStart(point);
          return;
        }
        const end = tool === "horizontal" ? { time: point.time, price: drawingStart.price } : tool === "vertical" ? { time: drawingStart.time, price: point.price } : point;
        setDrawings((current) => [...current, { tool, p1: drawingStart, p2: end }]);
        setDrawingStart(null);
      }}
    >
      {drawings.map((drawing, index) => {
        const p1 = toPixel(drawing.p1);
        const p2 = toPixel(drawing.p2);
        if (!p1 || !p2) return null;
        return <line key={`${drawing.tool}-${index}`} x1={p1.x} y1={p1.y} x2={p2.x} y2={p2.y} stroke="rgba(216,168,78,0.9)" strokeWidth="1" strokeDasharray={drawing.tool === "horizontal" || drawing.tool === "vertical" ? "4 3" : undefined} />;
      })}
      {startPixel && <circle cx={startPixel.x} cy={startPixel.y} r="3" fill="rgba(216,168,78,0.9)" />}
    </svg>
  );
}
