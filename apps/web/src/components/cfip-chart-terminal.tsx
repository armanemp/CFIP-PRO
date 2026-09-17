"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import {
  AreaSeries,
  BarSeries,
  BaselineSeries,
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

type ChartType = "candles" | "bars" | "line" | "area" | "baseline";
type Timeframe = "1m" | "5m" | "15m" | "1H" | "4H" | "1D";
type Indicator = "sma20" | "ema20" | "ema50" | "wma20" | "bb20" | "vwap";
type DrawingTool = "cursor" | "horizontal" | "vertical" | "trendline" | "rectangle";
type Overlay = "fvg" | "ob" | "structure";
interface Candle { time: UTCTimestamp; open: number; high: number; low: number; close: number; volume: number }
interface Zone { start: UTCTimestamp; end: UTCTimestamp; top: number; bottom: number; kind: "bullish" | "bearish" }
interface Point { time: UTCTimestamp; price: number }
interface Drawing { tool: Exclude<DrawingTool, "cursor">; a: Point; b: Point }

const TF: Record<Timeframe, number> = { "1m": 60, "5m": 300, "15m": 900, "1H": 3600, "4H": 14400, "1D": 86400 };
const TIMEFRAMES: Timeframe[] = ["1m", "5m", "15m", "1H", "4H", "1D"];
const INDICATORS: { id: Indicator; label: string }[] = [
  { id: "sma20", label: "SMA 20" }, { id: "ema20", label: "EMA 20" }, { id: "ema50", label: "EMA 50" },
  { id: "wma20", label: "WMA 20" }, { id: "bb20", label: "BB 20" }, { id: "vwap", label: "VWAP" },
];

function aggregate(observations: MarketObservation[], timeframe: Timeframe): Candle[] {
  const seconds = TF[timeframe];
  const sorted = observations.map((o) => ({ o, t: Math.floor(new Date(o.observed_at).getTime() / 1000) }))
    .filter(({ o, t }) => Number.isFinite(t) && (o.last ?? o.bid ?? o.ask) !== null).sort((a, b) => a.t - b.t);
  const buckets = new Map<number, Candle>();
  for (const { o, t } of sorted) {
    const raw = o.last ?? o.bid ?? o.ask;
    if (raw === null) continue;
    const price = Number(raw);
    if (!Number.isFinite(price)) continue;
    const key = Math.floor(t / seconds) * seconds;
    const volume = Number(o.volume ?? 0);
    const v = Number.isFinite(volume) && volume >= 0 ? volume : 0;
    const current = buckets.get(key);
    if (!current) buckets.set(key, { time: key as UTCTimestamp, open: price, high: price, low: price, close: price, volume: v });
    else { current.high = Math.max(current.high, price); current.low = Math.min(current.low, price); current.close = price; current.volume += v; }
  }
  return [...buckets.values()];
}

function sma(c: Candle[], p: number) { return c.flatMap((x, i) => i + 1 < p ? [] : [{ time: x.time, value: c.slice(i + 1 - p, i + 1).reduce((s, q) => s + q.close, 0) / p }]); }
function ema(c: Candle[], p: number) {
  if (c.length < p) return [] as { time: UTCTimestamp; value: number }[];
  const out: { time: UTCTimestamp; value: number }[] = []; let value = c.slice(0, p).reduce((s, x) => s + x.close, 0) / p; const k = 2 / (p + 1);
  out.push({ time: c[p - 1].time, value });
  for (let i = p; i < c.length; i += 1) { value += (c[i].close - value) * k; out.push({ time: c[i].time, value }); }
  return out;
}
function wma(c: Candle[], p: number) {
  const weight = (p * (p + 1)) / 2;
  return c.flatMap((x, i) => i + 1 < p ? [] : [{ time: x.time, value: c.slice(i + 1 - p, i + 1).reduce((s, q, j) => s + q.close * (j + 1), 0) / weight }]);
}
function bb(c: Candle[], p = 20, mult = 2) {
  return c.flatMap((x, i) => {
    if (i + 1 < p) return [];
    const values = c.slice(i + 1 - p, i + 1).map((q) => q.close); const mean = values.reduce((s, q) => s + q, 0) / p;
    const dev = Math.sqrt(values.reduce((s, q) => s + (q - mean) ** 2, 0) / p) * mult;
    return [{ time: x.time, upper: mean + dev, middle: mean, lower: mean - dev }];
  });
}
function vwap(c: Candle[]) {
  let pv = 0; let volume = 0;
  return c.map((x) => { const v = x.volume > 0 ? x.volume : 1; pv += ((x.high + x.low + x.close) / 3) * v; volume += v; return { time: x.time, value: pv / volume }; });
}
function rsi(c: Candle[], p = 14) {
  if (c.length <= p) return [] as { time: UTCTimestamp; value: number }[];
  let gain = 0; let loss = 0;
  for (let i = 1; i <= p; i += 1) { const d = c[i].close - c[i - 1].close; gain += Math.max(d, 0); loss += Math.max(-d, 0); }
  let ag = gain / p; let al = loss / p; const out = [{ time: c[p].time, value: al === 0 ? 100 : 100 - 100 / (1 + ag / al) }];
  for (let i = p + 1; i < c.length; i += 1) { const d = c[i].close - c[i - 1].close; ag = (ag * (p - 1) + Math.max(d, 0)) / p; al = (al * (p - 1) + Math.max(-d, 0)) / p; out.push({ time: c[i].time, value: al === 0 ? 100 : 100 - 100 / (1 + ag / al) }); }
  return out;
}
function macd(c: Candle[]) {
  const fast = ema(c, 12); const slow = ema(c, 26); const slowMap = new Map(slow.map((x) => [Number(x.time), x.value]));
  const line = fast.flatMap((x) => { const s = slowMap.get(Number(x.time)); return s === undefined ? [] : [{ time: x.time, value: x.value - s }]; });
  const signal = line.length < 9 ? [] : ema(line.map((x) => ({ time: x.time, open: x.value, high: x.value, low: x.value, close: x.value, volume: 0 })), 9);
  return { line, signal };
}
function zones(c: Candle[], kind: "fvg" | "ob"): Zone[] {
  const out: Zone[] = []; const end = c.at(-1)?.time; if (end === undefined) return out;
  if (kind === "fvg") {
    for (let i = 2; i < c.length; i += 1) {
      const a = c[i - 2]; const b = c[i];
      if (a.high < b.low) out.push({ start: c[i - 1].time, end, bottom: a.high, top: b.low, kind: "bullish" });
      else if (a.low > b.high) out.push({ start: c[i - 1].time, end, bottom: b.high, top: a.low, kind: "bearish" });
    }
  } else {
    for (let i = 1; i < c.length; i += 1) {
      const previous = c[i - 1]; const current = c[i]; const range = Math.max(current.high - current.low, Number.EPSILON);
      if (Math.abs(current.close - current.open) / range < 0.6) continue;
      if (current.close > current.open && previous.close < previous.open) out.push({ start: previous.time, end, top: previous.high, bottom: previous.low, kind: "bullish" });
      if (current.close < current.open && previous.close > previous.open) out.push({ start: previous.time, end, top: previous.high, bottom: previous.low, kind: "bearish" });
    }
  }
  return out.slice(-10);
}
function structure(c: Candle[]) {
  const out: { time: UTCTimestamp; price: number; label: string }[] = [];
  for (let i = 2; i < c.length - 2; i += 1) {
    if (c[i].high > c[i - 1].high && c[i].high >= c[i + 1].high) out.push({ time: c[i].time, price: c[i].high, label: "HH/SH" });
    if (c[i].low < c[i - 1].low && c[i].low <= c[i + 1].low) out.push({ time: c[i].time, price: c[i].low, label: "LL/SL" });
  }
  return out.slice(-16);
}

const chartOptions = {
  autoSize: true,
  layout: { background: { type: ColorType.Solid, color: "#080b10" }, textColor: "#a7b0be", attributionLogo: false, panes: { separatorColor: "#1b2430", separatorHoverColor: "#334154", enableResize: true } },
  grid: { vertLines: { color: "#121923" }, horzLines: { color: "#121923" } },
  rightPriceScale: { borderColor: "#25303d", autoScale: true }, leftPriceScale: { visible: false, borderColor: "#25303d" }, defaultVisiblePriceScaleId: "right",
  timeScale: { borderColor: "#25303d", timeVisible: true, secondsVisible: false, rightOffset: 8, barSpacing: 7, minBarSpacing: 2, maxBarSpacing: 20 },
  crosshair: { mode: CrosshairMode.Normal, vertLine: { color: "#536273", width: 1, style: 3, labelBackgroundColor: "#26364a" }, horzLine: { color: "#536273", width: 1, style: 3, labelBackgroundColor: "#26364a" } },
  handleScroll: { mouseWheel: true, pressedMouseMove: true, horzTouchDrag: true, vertTouchDrag: true },
  handleScale: { mouseWheel: true, pinch: true, axisPressedMouseMove: true, axisDoubleClickReset: true },
  kineticScroll: { mouse: true, touch: true }, hoveredSeriesOnTop: true,
} as const;

interface Props { observations: MarketObservation[]; symbol?: string; }

export function CfipChartTerminal({ observations, symbol = "EUR/USD" }: Props) {
  const hostRef = useRef<HTMLDivElement>(null); const chartRef = useRef<IChartApi | null>(null); const mainRef = useRef<ISeriesApi<SeriesType> | null>(null); const seriesRef = useRef<ISeriesApi<SeriesType>[]>([]); const drawingHostRef = useRef<HTMLDivElement>(null);
  const [timeframe, setTimeframe] = useState<Timeframe>("15m"); const [chartType, setChartType] = useState<ChartType>("candles"); const [indicators, setIndicators] = useState<Indicator[]>(["ema20"]); const [overlays, setOverlays] = useState<Overlay[]>([]); const [volume, setVolume] = useState(true); const [rsiPane, setRsiPane] = useState(false); const [macdPane, setMacdPane] = useState(false); const [tool, setTool] = useState<DrawingTool>("cursor"); const [drawings, setDrawings] = useState<Drawing[]>([]); const [draft, setDraft] = useState<Point | null>(null); const [leftScale, setLeftScale] = useState(false); const [autoScale, setAutoScale] = useState(true); const [logScale, setLogScale] = useState(false); const [menu, setMenu] = useState<"indicators" | "drawings" | "settings" | null>(null); const [fullscreen, setFullscreen] = useState(false); const [cursor, setCursor] = useState<{ time?: string; price?: number }>({});
  const candles = useMemo(() => aggregate(observations, timeframe), [observations, timeframe]); const fvg = useMemo(() => zones(candles, "fvg"), [candles]); const ob = useMemo(() => zones(candles, "ob"), [candles]); const structurePoints = useMemo(() => structure(candles), [candles]); const latest = candles.at(-1); const previous = candles.at(-2); const change = latest && previous ? latest.close - previous.close : 0; const pct = previous?.close ? (change / previous.close) * 100 : 0;

  useEffect(() => {
    const host = hostRef.current; if (!host) return;
    const chart = createChart(host, chartOptions); chartRef.current = chart;
    const handler = (param: { time?: unknown; point?: { x: number; y: number } }) => { if (!param.point || !mainRef.current) return; const price = mainRef.current.coordinateToPrice(param.point.y); setCursor({ time: param.time ? String(param.time) : undefined, price: price ?? undefined }); };
    chart.subscribeCrosshairMove(handler);
    return () => { chart.unsubscribeCrosshairMove(handler); chart.remove(); chartRef.current = null; };
  }, []);

  useEffect(() => {
    const chart = chartRef.current; if (!chart || candles.length === 0) return;
    for (const s of seriesRef.current) chart.removeSeries(s); seriesRef.current = [];
    for (let i = chart.panes().length - 1; i > 0; i -= 1) chart.removePane(i);
    const add = <T extends SeriesType>(s: ISeriesApi<T>) => { seriesRef.current.push(s as ISeriesApi<SeriesType>); return s; };
    const common = { priceScaleId: leftScale ? "left" : "right" } as const;
    let main: ISeriesApi<SeriesType>;
    if (chartType === "candles") main = add(chart.addSeries(CandlestickSeries, { ...common, upColor: "#20c997", downColor: "#ef6461", borderUpColor: "#20c997", borderDownColor: "#ef6461", wickUpColor: "#20c997", wickDownColor: "#ef6461", lastValueVisible: true }));
    else if (chartType === "bars") main = add(chart.addSeries(BarSeries, { ...common, upColor: "#20c997", downColor: "#ef6461" }));
    else if (chartType === "area") main = add(chart.addSeries(AreaSeries, { ...common, lineColor: "#4da3ff", lineWidth: 2, topColor: "rgba(77,163,255,0.25)", bottomColor: "rgba(77,163,255,0.01)" }));
    else if (chartType === "baseline") main = add(chart.addSeries(BaselineSeries, { ...common, baseValue: { type: "price", price: candles[0].close }, topLineColor: "#20c997", topFillColor1: "rgba(32,201,151,0.20)", topFillColor2: "rgba(32,201,151,0.02)", bottomLineColor: "#ef6461", bottomFillColor1: "rgba(239,100,97,0.02)", bottomFillColor2: "rgba(239,100,97,0.20)" }));
    else main = add(chart.addSeries(LineSeries, { ...common, color: "#4da3ff", lineWidth: 2 }));
    main.setData(chartType === "candles" || chartType === "bars" ? candles : candles.map((x) => ({ time: x.time, value: x.close })));
    mainRef.current = main;
    main.priceScale().applyOptions({ autoScale, mode: logScale ? 1 : 0, scaleMargins: { top: 0.06, bottom: volume ? 0.08 : 0.04 } });

    if (volume) { const s = add(chart.addSeries(HistogramSeries, { priceScaleId: "volume", priceFormat: { type: "volume" }, color: "rgba(104,119,138,0.45)" }, 1)); s.priceScale().applyOptions({ scaleMargins: { top: 0.25, bottom: 0 } }); s.setData(candles.map((x) => ({ time: x.time, value: x.volume, color: x.close >= x.open ? "rgba(32,201,151,0.42)" : "rgba(239,100,97,0.42)" })));
    }
    if (rsiPane) { const s = add(chart.addSeries(LineSeries, { color: "#c084fc", lineWidth: 2, title: "RSI 14", priceScaleId: "rsi" }, volume ? 2 : 1)); s.setData(rsi(candles)); s.priceScale().applyOptions({ autoScale: false, scaleMargins: { top: 0.08, bottom: 0.08 } }); }
    if (macdPane) { const pane = (volume ? 1 : 0) + (rsiPane ? 1 : 0) + 1; const m = macd(candles); const a = add(chart.addSeries(LineSeries, { color: "#4da3ff", lineWidth: 2, title: "MACD", priceScaleId: "macd" }, pane)); const b = add(chart.addSeries(LineSeries, { color: "#f5b942", lineWidth: 1, title: "Signal", priceScaleId: "macd" }, pane)); a.setData(m.line); b.setData(m.signal); }

    const lineColors: Record<Indicator, string> = { sma20: "#f5b942", ema20: "#4da3ff", ema50: "#c084fc", wma20: "#fb923c", vwap: "#22d3ee", bb20: "#94a3b8" };
    for (const id of indicators) {
      if (id === "bb20") { const bands = bb(candles); for (const key of ["upper", "middle", "lower"] as const) { const s = add(chart.addSeries(LineSeries, { color: lineColors[id], lineWidth: key === "middle" ? 1 : 1, lineStyle: key === "middle" ? 0 : 2, priceScaleId: leftScale ? "left" : "right", title: `BB ${key}` })); s.setData(bands.map((x) => ({ time: x.time, value: x[key] }))); } }
      else { const data = id === "sma20" ? sma(candles, 20) : id === "ema20" ? ema(candles, 20) : id === "ema50" ? ema(candles, 50) : id === "wma20" ? wma(candles, 20) : vwap(candles); const s = add(chart.addSeries(LineSeries, { color: lineColors[id], lineWidth: 2, priceScaleId: leftScale ? "left" : "right", title: id.toUpperCase() })); s.setData(data); }
    }
    chart.applyOptions({ leftPriceScale: { visible: leftScale }, rightPriceScale: { visible: true, autoScale }, defaultVisiblePriceScaleId: leftScale ? "left" : "right" });
    chart.timeScale().fitContent();
  }, [candles, chartType, indicators, overlays, volume, rsiPane, macdPane, leftScale, autoScale, logScale]);

  const pointFromPointer = (event: React.PointerEvent<HTMLDivElement>): Point | null => {
    const chart = chartRef.current; const series = mainRef.current; const host = drawingHostRef.current; if (!chart || !series || !host) return null;
    const rect = host.getBoundingClientRect(); const x = event.clientX - rect.left; const y = event.clientY - rect.top; const time = chart.timeScale().coordinateToTime(x); const price = series.coordinateToPrice(y);
    if (time === null || price === null) return null; return { time: time as UTCTimestamp, price };
  };
  const onPointerDown = (event: React.PointerEvent<HTMLDivElement>) => { if (tool === "cursor") return; const point = pointFromPointer(event); if (!point) return; if (tool === "horizontal" || tool === "vertical") { setDrawings((items) => [...items, { tool, a: point, b: point }]); return; } setDraft(point); };
  const onPointerUp = (event: React.PointerEvent<HTMLDivElement>) => { if (!draft || tool === "cursor") return; const point = pointFromPointer(event); if (point) setDrawings((items) => [...items, { tool, a: draft, b: point }]); setDraft(null); };
  const clearDrawings = () => { setDrawings([]); setDraft(null); };
  const toggleIndicator = (id: Indicator) => setIndicators((items) => items.includes(id) ? items.filter((x) => x !== id) : [...items, id]);
  const toggleOverlay = (id: Overlay) => setOverlays((items) => items.includes(id) ? items.filter((x) => x !== id) : [...items, id]);
  const downloadScreenshot = () => { const chart = chartRef.current; if (!chart) return; const canvas = chart.takeScreenshot(true, false); const link = document.createElement("a"); link.download = `cfip-${symbol.replace("/", "-")}.png`; link.href = canvas.toDataURL("image/png"); link.click(); };
  const resetView = () => chartRef.current?.timeScale().fitContent();
  const toggleFullscreen = async () => { const element = hostRef.current?.parentElement; if (!element) return; if (document.fullscreenElement) { await document.exitFullscreen(); setFullscreen(false); } else { await element.requestFullscreen(); setFullscreen(true); } };

  return <div className="flex h-full min-h-0 flex-col bg-[#080b10] text-[#d8dee8]">
    <div className="flex h-10 shrink-0 items-center gap-1 border-b border-[#1c2632] bg-[#0c1118] px-2 text-[11px]">
      <div className="mr-2 flex items-center gap-2"><span className="font-semibold tracking-wide text-white">{symbol}</span><span className={pct >= 0 ? "text-emerald-400" : "text-red-400"}>{latest ? latest.close.toFixed(5) : "—"}</span><span className={pct >= 0 ? "text-emerald-400" : "text-red-400"}>{latest ? `${change >= 0 ? "+" : ""}${pct.toFixed(2)}%` : ""}</span></div>
      {TIMEFRAMES.map((tf) => <button key={tf} onClick={() => setTimeframe(tf)} className={`rounded px-2 py-1 ${tf === timeframe ? "bg-[#253244] text-white" : "text-[#8995a6] hover:bg-[#18212c] hover:text-white"}`}>{tf}</button>)}
      <span className="mx-1 h-5 w-px bg-[#25303d]" />
      {(["candles", "bars", "line", "area", "baseline"] as ChartType[]).map((type) => <button key={type} title={type} onClick={() => setChartType(type)} className={`rounded px-2 py-1 ${type === chartType ? "bg-[#253244] text-white" : "text-[#8995a6] hover:bg-[#18212c] hover:text-white"}`}>{type === "candles" ? "C" : type[0].toUpperCase()}</button>)}
      <span className="mx-1 h-5 w-px bg-[#25303d]" />
      <button onClick={() => setMenu(menu === "indicators" ? null : "indicators")} className="rounded px-2 py-1 text-[#aab5c4] hover:bg-[#18212c]">Indicators</button>
      <button onClick={() => setMenu(menu === "drawings" ? null : "drawings")} className="rounded px-2 py-1 text-[#aab5c4] hover:bg-[#18212c]">Draw</button>
      <button onClick={() => setVolume(!volume)} className={`rounded px-2 py-1 ${volume ? "text-white" : "text-[#667384]"}`}>Vol</button>
      <button onClick={() => setRsiPane(!rsiPane)} className={`rounded px-2 py-1 ${rsiPane ? "text-white" : "text-[#667384]"}`}>RSI</button>
      <button onClick={() => setMacdPane(!macdPane)} className={`rounded px-2 py-1 ${macdPane ? "text-white" : "text-[#667384]"}`}>MACD</button>
      <div className="ml-auto flex items-center gap-1"><button onClick={resetView} className="rounded px-2 py-1 text-[#aab5c4] hover:bg-[#18212c]">Fit</button><button onClick={() => setAutoScale(!autoScale)} className={`rounded px-2 py-1 ${autoScale ? "text-white" : "text-[#667384]"}`}>Auto</button><button onClick={() => setLeftScale(!leftScale)} className={`rounded px-2 py-1 ${leftScale ? "text-white" : "text-[#667384]"}`}>L/R</button><button onClick={() => setLogScale(!logScale)} className={`rounded px-2 py-1 ${logScale ? "text-white" : "text-[#667384]"}`}>Log</button><button onClick={downloadScreenshot} className="rounded px-2 py-1 text-[#aab5c4] hover:bg-[#18212c]">Shot</button><button onClick={toggleFullscreen} className="rounded px-2 py-1 text-[#aab5c4] hover:bg-[#18212c]">{fullscreen ? "Exit" : "Full"}</button></div>
    </div>
    {menu === "indicators" && <div className="absolute z-30 mt-10 ml-40 flex max-w-[420px] flex-wrap gap-1 rounded border border-[#263342] bg-[#0e141c] p-2 shadow-2xl">{INDICATORS.map((item) => <button key={item.id} onClick={() => toggleIndicator(item.id)} className={`rounded px-2 py-1 text-[11px] ${indicators.includes(item.id) ? "bg-[#253244] text-white" : "text-[#8995a6]"}`}>{item.label}</button>)}</div>}
    {menu === "drawings" && <div className="absolute z-30 mt-10 ml-56 flex flex-wrap gap-1 rounded border border-[#263342] bg-[#0e141c] p-2 shadow-2xl"><button onClick={() => setTool("cursor")} className="rounded px-2 py-1 text-[11px]">Cursor</button><button onClick={() => setTool("horizontal")} className="rounded px-2 py-1 text-[11px]">H-Line</button><button onClick={() => setTool("vertical")} className="rounded px-2 py-1 text-[11px]">V-Line</button><button onClick={() => setTool("trendline")} className="rounded px-2 py-1 text-[11px]">Trend</button><button onClick={() => setTool("rectangle")} className="rounded px-2 py-1 text-[11px]">Rect</button><button onClick={clearDrawings} className="rounded px-2 py-1 text-red-300">Clear</button></div>}
    <div ref={drawingHostRef} className="relative min-h-0 flex-1" onPointerDown={onPointerDown} onPointerUp={onPointerUp}>
      <div ref={hostRef} className="absolute inset-0" />
      {cursor.price !== undefined && <div className="pointer-events-none absolute left-3 top-3 z-10 rounded bg-[#0c1118cc] px-2 py-1 font-mono text-[10px] text-[#aeb9c8]">{cursor.price.toFixed(5)}{cursor.time ? ` · ${cursor.time}` : ""}</div>}
      {overlays.includes("fvg") && <div className="pointer-events-none absolute left-3 bottom-12 z-10 rounded bg-[#0c1118cc] px-2 py-1 text-[10px] text-cyan-300">FVG analysis enabled · {fvg.length} zones</div>}
      {overlays.includes("ob") && <div className="pointer-events-none absolute left-3 bottom-7 z-10 rounded bg-[#0c1118cc] px-2 py-1 text-[10px] text-amber-300">Order Block analysis enabled · {ob.length} zones</div>}
      {overlays.includes("structure") && <div className="pointer-events-none absolute left-3 bottom-2 z-10 rounded bg-[#0c1118cc] px-2 py-1 text-[10px] text-violet-300">Structure enabled · {structurePoints.length} pivots</div>}
      <svg className="pointer-events-none absolute inset-0 z-20 h-full w-full overflow-visible">{drawings.map((d, i) => { const chart = chartRef.current; const series = mainRef.current; if (!chart || !series) return null; const x1 = chart.timeScale().timeToCoordinate(d.a.time); const x2 = chart.timeScale().timeToCoordinate(d.b.time); const y1 = series.priceToCoordinate(d.a.price); const y2 = series.priceToCoordinate(d.b.price); if (x1 === null || x2 === null || y1 === null || y2 === null) return null; if (d.tool === "horizontal") return <line key={i} x1={0} x2="100%" y1={y1} y2={y1} stroke="#f5b942" strokeWidth="1" strokeDasharray="4 4" />; if (d.tool === "vertical") return <line key={i} x1={x1} x2={x1} y1={0} y2="100%" stroke="#f5b942" strokeWidth="1" strokeDasharray="4 4" />; if (d.tool === "rectangle") return <rect key={i} x={Math.min(x1, x2)} y={Math.min(y1, y2)} width={Math.abs(x2 - x1)} height={Math.abs(y2 - y1)} fill="rgba(77,163,255,0.08)" stroke="#4da3ff" strokeWidth="1" />; return <line key={i} x1={x1} x2={x2} y1={y1} y2={y2} stroke="#4da3ff" strokeWidth="1.5" />; })}</svg>
    </div>
  </div>;
}
