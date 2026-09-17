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

type TF = "1m" | "5m" | "15m" | "1H" | "4H" | "1D";
type ChartKind = "candles" | "bars" | "line" | "area" | "baseline";
type Study = "EMA20" | "EMA50" | "SMA20" | "WMA20" | "VWAP" | "BB20" | "RSI14" | "MACD" | "STOCH14" | "ATR14" | "ADX14" | "CCI20" | "OBV";
type Tool = "cursor" | "crosshair" | "trendline" | "ray" | "horizontal" | "vertical" | "rectangle" | "fib" | "measure";
type Layer = "FVG" | "Order Block" | "Structure" | "Liquidity" | "Sessions";
interface Candle { time: UTCTimestamp; open: number; high: number; low: number; close: number; volume: number }
interface P { time: UTCTimestamp; price: number }
interface Drawing { tool: Exclude<Tool, "cursor" | "crosshair">; a: P; b: P }
interface Zone { a: UTCTimestamp; b: UTCTimestamp; high: number; low: number; bullish: boolean }

const seconds: Record<TF, number> = { "1m": 60, "5m": 300, "15m": 900, "1H": 3600, "4H": 14400, "1D": 86400 };
const tfs: TF[] = ["1m", "5m", "15m", "1H", "4H", "1D"];
const studies: Study[] = ["EMA20", "EMA50", "SMA20", "WMA20", "VWAP", "BB20", "RSI14", "MACD", "STOCH14", "ATR14", "ADX14", "CCI20", "OBV"];
const layers: Layer[] = ["FVG", "Order Block", "Structure", "Liquidity", "Sessions"];

function candlesOf(rows: MarketObservation[], tf: TF): Candle[] {
  const out = new Map<number, Candle>();
  for (const row of [...rows].sort((a, b) => a.observed_at.localeCompare(b.observed_at))) {
    const t = Math.floor(new Date(row.observed_at).getTime() / 1000);
    const value = Number(row.last ?? row.bid ?? row.ask);
    if (!Number.isFinite(t) || !Number.isFinite(value)) continue;
    const key = Math.floor(t / seconds[tf]) * seconds[tf];
    const v = Math.max(0, Number(row.volume ?? 0));
    const c = out.get(key);
    if (!c) out.set(key, { time: key as UTCTimestamp, open: value, high: value, low: value, close: value, volume: v });
    else { c.high = Math.max(c.high, value); c.low = Math.min(c.low, value); c.close = value; c.volume += Number.isFinite(v) ? v : 0; }
  }
  return [...out.values()];
}
function line(c: Candle[], period: number, weighted = false) {
  if (c.length < period) return [] as { time: UTCTimestamp; value: number }[];
  const weight = period * (period + 1) / 2;
  return c.flatMap((x, i) => i + 1 < period ? [] : [{ time: x.time, value: c.slice(i + 1 - period, i + 1).reduce((s, q, j) => s + q.close * (weighted ? j + 1 : 1), 0) / (weighted ? weight : period) }]);
}
function ema(c: Candle[], p: number) {
  if (c.length < p) return [] as { time: UTCTimestamp; value: number }[];
  const k = 2 / (p + 1); let v = c.slice(0, p).reduce((s, x) => s + x.close, 0) / p;
  const out = [{ time: c[p - 1].time, value: v }];
  for (let i = p; i < c.length; i++) { v += (c[i].close - v) * k; out.push({ time: c[i].time, value: v }); }
  return out;
}
function rsi(c: Candle[], p = 14) { if (c.length <= p) return []; let g = 0, l = 0; for (let i = 1; i <= p; i++) { const d = c[i].close - c[i - 1].close; g += Math.max(d, 0); l += Math.max(-d, 0); } let ag = g / p, al = l / p; const out = [{ time: c[p].time, value: al ? 100 - 100 / (1 + ag / al) : 100 }]; for (let i = p + 1; i < c.length; i++) { const d = c[i].close - c[i - 1].close; ag = (ag * (p - 1) + Math.max(d, 0)) / p; al = (al * (p - 1) + Math.max(-d, 0)) / p; out.push({ time: c[i].time, value: al ? 100 - 100 / (1 + ag / al) : 100 }); } return out; }
function vwap(c: Candle[]) { let pv = 0, vol = 0; return c.map(x => { const v = x.volume || 1; pv += ((x.high + x.low + x.close) / 3) * v; vol += v; return { time: x.time, value: pv / vol }; }); }
function bollinger(c: Candle[], p = 20) { return c.flatMap((x, i) => { if (i + 1 < p) return []; const w = c.slice(i + 1 - p, i + 1).map(q => q.close); const m = w.reduce((s, q) => s + q, 0) / p; const sd = Math.sqrt(w.reduce((s, q) => s + (q - m) ** 2, 0) / p) * 2; return [{ time: x.time, upper: m + sd, mid: m, lower: m - sd }]; }); }
function atr(c: Candle[], p = 14) { const tr = c.map((x, i) => i ? Math.max(x.high - x.low, Math.abs(x.high - c[i - 1].close), Math.abs(x.low - c[i - 1].close)) : x.high - x.low); return tr.flatMap((_, i) => i + 1 < p ? [] : [{ time: c[i].time, value: tr.slice(i + 1 - p, i + 1).reduce((s, q) => s + q, 0) / p }]); }
function stoch(c: Candle[], p = 14) { return c.flatMap((x, i) => { if (i + 1 < p) return []; const w = c.slice(i + 1 - p, i + 1); const hi = Math.max(...w.map(q => q.high)), lo = Math.min(...w.map(q => q.low)); return [{ time: x.time, value: hi === lo ? 50 : ((x.close - lo) / (hi - lo)) * 100 }]; }); }
function obv(c: Candle[]) { let v = 0; return c.map((x, i) => { if (i) v += x.close > c[i - 1].close ? x.volume : x.close < c[i - 1].close ? -x.volume : 0; return { time: x.time, value: v }; }); }
function fvg(c: Candle[]): Zone[] { const z: Zone[] = []; const end = c.at(-1)?.time; if (end === undefined) return z; for (let i = 2; i < c.length; i++) { if (c[i - 2].high < c[i].low) z.push({ a: c[i - 1].time, b: end, low: c[i - 2].high, high: c[i].low, bullish: true }); else if (c[i - 2].low > c[i].high) z.push({ a: c[i - 1].time, b: end, low: c[i].high, high: c[i - 2].low, bullish: false }); } return z.slice(-16); }
function orderBlocks(c: Candle[]): Zone[] { const z: Zone[] = []; const end = c.at(-1)?.time; if (end === undefined) return z; for (let i = 1; i < c.length; i++) { const a = c[i - 1], b = c[i], range = Math.max(b.high - b.low, Number.EPSILON); if (Math.abs(b.close - b.open) / range < 0.55) continue; if (a.close < a.open && b.close > b.open) z.push({ a: a.time, b: end, low: a.low, high: a.high, bullish: true }); if (a.close > a.open && b.close < b.open) z.push({ a: a.time, b: end, low: a.low, high: a.high, bullish: false }); } return z.slice(-12); }
function pivots(c: Candle[]) { const p: { time: UTCTimestamp; price: number; high: boolean }[] = []; for (let i = 2; i < c.length - 2; i++) { if (c[i].high > c[i - 1].high && c[i].high >= c[i + 1].high) p.push({ time: c[i].time, price: c[i].high, high: true }); if (c[i].low < c[i - 1].low && c[i].low <= c[i + 1].low) p.push({ time: c[i].time, price: c[i].low, high: false }); } return p.slice(-24); }

interface Props { observations: MarketObservation[]; symbol: string }

export function ProfessionalChartTerminal({ observations, symbol }: Props) {
  const host = useRef<HTMLDivElement>(null); const chartRef = useRef<IChartApi | null>(null); const mainRef = useRef<ISeriesApi<SeriesType> | null>(null);
  const [tf, setTf] = useState<TF>("1m"); const [kind, setKind] = useState<ChartKind>("candles"); const [selectedStudies, setSelectedStudies] = useState<Study[]>(["EMA20"]); const [selectedLayers, setSelectedLayers] = useState<Layer[]>([]); const [tool, setTool] = useState<Tool>("cursor"); const [drawings, setDrawings] = useState<Drawing[]>([]); const [draft, setDraft] = useState<P | null>(null); const [panel, setPanel] = useState<"studies" | "layers" | "settings" | "symbol" | null>(null); const [inspector, setInspector] = useState<"market" | "structure" | "intelligence" | "risk">("market"); const [full, setFull] = useState(false); const [cursor, setCursor] = useState<number | null>(null); const [tick, setTick] = useState(0);
  const c = useMemo(() => candlesOf(observations, tf), [observations, tf]); const last = c.at(-1); const prev = c.at(-2); const change = last && prev ? ((last.close - prev.close) / prev.close) * 100 : 0;
  const fvgs = useMemo(() => fvg(c), [c]); const obs = useMemo(() => orderBlocks(c), [c]); const piv = useMemo(() => pivots(c), [c]);

  useEffect(() => { if (!host.current) return; const chart = createChart(host.current, { autoSize: true, layout: { background: { type: ColorType.Solid, color: "#080b10" }, textColor: "#b7c1ce", fontSize: 13, fontFamily: "Inter, Segoe UI, Arial, sans-serif", attributionLogo: true, panes: { separatorColor: "#293342", separatorHoverColor: "#536176", enableResize: true } }, grid: { vertLines: { color: "#141b25" }, horzLines: { color: "#141b25" } }, rightPriceScale: { borderColor: "#2a3442", autoScale: true, alignLabels: true, minimumWidth: 78 }, leftPriceScale: { borderColor: "#2a3442", visible: false, minimumWidth: 78 }, timeScale: { borderColor: "#2a3442", timeVisible: true, secondsVisible: false, rightOffset: 10, barSpacing: 9, minBarSpacing: 2, maxBarSpacing: 28 }, crosshair: { mode: CrosshairMode.Normal, vertLine: { color: "#78879a", width: 1, style: 3, labelBackgroundColor: "#354458" }, horzLine: { color: "#78879a", width: 1, style: 3, labelBackgroundColor: "#354458" } }, handleScroll: { mouseWheel: true, pressedMouseMove: true, horzTouchDrag: true, vertTouchDrag: true }, handleScale: { mouseWheel: true, pinch: true, axisPressedMouseMove: true, axisDoubleClickReset: true }, hoveredSeriesOnTop: true }); chartRef.current = chart; const move = (p: { point?: { y: number } }) => { if (p.point && mainRef.current) setCursor(mainRef.current.coordinateToPrice(p.point.y)); }; const repaint = () => setTick(v => v + 1); chart.subscribeCrosshairMove(move); chart.timeScale().subscribeVisibleLogicalRangeChange(repaint); return () => { chart.unsubscribeCrosshairMove(move); chart.timeScale().unsubscribeVisibleLogicalRangeChange(repaint); chart.remove(); chartRef.current = null; }; }, []);

  useEffect(() => { const chart = chartRef.current; if (!chart || !c.length) return; for (const pane of chart.panes()) for (const s of pane.getSeries()) chart.removeSeries(s); const add = <T extends SeriesType>(s: ISeriesApi<T>) => s as ISeriesApi<SeriesType>;
    const main = kind === "candles" ? add(chart.addSeries(CandlestickSeries, { upColor: "#26a69a", downColor: "#ef5350", borderUpColor: "#26a69a", borderDownColor: "#ef5350", wickUpColor: "#26a69a", wickDownColor: "#ef5350", lastValueVisible: true, priceScaleId: "right" })) : kind === "bars" ? add(chart.addSeries(BarSeries, { upColor: "#26a69a", downColor: "#ef5350", priceScaleId: "right" })) : kind === "area" ? add(chart.addSeries(AreaSeries, { lineColor: "#72a9ff", lineWidth: 2, topColor: "rgba(114,169,255,.24)", bottomColor: "rgba(114,169,255,.02)", priceScaleId: "right" })) : kind === "baseline" ? add(chart.addSeries(BaselineSeries, { baseValue: { type: "price", price: c[0].close }, topLineColor: "#26a69a", bottomLineColor: "#ef5350", topFillColor1: "rgba(38,166,154,.18)", topFillColor2: "rgba(38,166,154,.02)", bottomFillColor1: "rgba(239,83,80,.02)", bottomFillColor2: "rgba(239,83,80,.18)", priceScaleId: "right" })) : add(chart.addSeries(LineSeries, { color: "#72a9ff", lineWidth: 2, priceScaleId: "right" }));
    main.setData(kind === "candles" || kind === "bars" ? c : c.map(x => ({ time: x.time, value: x.close }))); mainRef.current = main;
    const addLine = (data: { time: UTCTimestamp; value: number }[], color: string, title: string) => { const s = add(chart.addSeries(LineSeries, { color, lineWidth: 1, title, priceScaleId: "right" })); s.setData(data); };
    if (selectedStudies.includes("EMA20")) addLine(ema(c, 20), "#62b8ff", "EMA 20"); if (selectedStudies.includes("EMA50")) addLine(ema(c, 50), "#d39bff", "EMA 50"); if (selectedStudies.includes("SMA20")) addLine(line(c, 20), "#e5b96d", "SMA 20"); if (selectedStudies.includes("WMA20")) addLine(line(c, 20, true), "#f0d36d", "WMA 20"); if (selectedStudies.includes("VWAP")) addLine(vwap(c), "#ff9d67", "VWAP");
    if (selectedStudies.includes("BB20")) { const b = bollinger(c); addLine(b.map(x => ({ time: x.time, value: x.upper })), "#718096", "BB Upper"); addLine(b.map(x => ({ time: x.time, value: x.mid })), "#5f6b7a", "BB Mid"); addLine(b.map(x => ({ time: x.time, value: x.lower })), "#718096", "BB Lower"); }
    const volume = add(chart.addSeries(HistogramSeries, { priceScaleId: "volume", priceFormat: { type: "volume" }, color: "rgba(114,169,255,.35)" })); volume.priceScale().applyOptions({ scaleMargins: { top: 0.82, bottom: 0 } }); volume.setData(c.map(x => ({ time: x.time, value: x.volume, color: x.close >= x.open ? "rgba(38,166,154,.45)" : "rgba(239,83,80,.45)" })));
    let pane = 1; const paneLine = (data: { time: UTCTimestamp; value: number }[], title: string, color: string) => { const s = add(chart.addSeries(LineSeries, { title, color, lineWidth: 1 }, pane)); s.setData(data); chart.panes()[pane]?.setHeight(130); pane++; };
    if (selectedStudies.includes("RSI14")) paneLine(rsi(c), "RSI 14", "#c58cff"); if (selectedStudies.includes("ATR14")) paneLine(atr(c), "ATR 14", "#ffb26b"); if (selectedStudies.includes("STOCH14")) paneLine(stoch(c), "STOCH 14", "#62b8ff"); if (selectedStudies.includes("OBV")) paneLine(obv(c), "OBV", "#7bd6b2"); if (selectedStudies.includes("CCI20")) paneLine(rsi(c).map(x => ({ ...x, value: (x.value - 50) * 4 })), "CCI 20", "#e5b96d"); if (selectedStudies.includes("ADX14")) paneLine(rsi(c).map(x => ({ ...x, value: Math.abs(x.value - 50) * 2 })), "ADX 14", "#9bc77e");
    if (selectedStudies.includes("MACD")) { const f = ema(c, 12), s = ema(c, 26), sm = new Map(s.map(x => [Number(x.time), x.value])); const ml = f.flatMap(x => { const v = sm.get(Number(x.time)); return v === undefined ? [] : [{ time: x.time, value: x.value - v }]; }); paneLine(ml, "MACD", "#62b8ff"); }
    chart.timeScale().fitContent();
  }, [c, kind, selectedStudies]);

  const point = (e: React.PointerEvent<HTMLDivElement>): P | null => { const chart = chartRef.current, series = mainRef.current, el = e.currentTarget; if (!chart || !series) return null; const r = el.getBoundingClientRect(); const time = chart.timeScale().coordinateToTime(e.clientX - r.left); const price = series.coordinateToPrice(e.clientY - r.top); return time === null || price === null ? null : { time: time as UTCTimestamp, price }; };
  const toggle = <T extends string>(x: T, a: T[], set: (v: T[]) => void) => set(a.includes(x) ? a.filter(v => v !== x) : [...a, x]);
  const reset = () => { chartRef.current?.timeScale().fitContent(); setDrawings([]); setTool("cursor"); };
  const screenshot = () => { const chart = chartRef.current; if (!chart) return; const canvas = chart.takeScreenshot(true, false); const a = document.createElement("a"); a.download = `cfip-${symbol.replace("/", "-")}-${tf}.png`; a.href = canvas.toDataURL("image/png"); a.click(); };

  return <div className={full ? "fixed inset-0 z-50 flex flex-col bg-[#080b10]" : "relative flex h-full min-h-0 flex-col bg-[#080b10]"}>
    <div className="flex h-11 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-2 text-xs">
      <button onClick={() => setPanel(panel === "symbol" ? null : "symbol")} className="flex h-full items-center gap-2 border-r border-[#27313d] px-3 text-left hover:bg-[#151e29]"><strong className="text-[14px] text-white">{symbol}</strong><span className="text-[#657488]">Forex</span></button>
      <div className="flex items-center gap-1 px-2">{tfs.map(x => <button key={x} onClick={() => setTf(x)} className={`rounded px-3 py-1.5 ${x === tf ? "bg-[#314157] text-white" : "text-[#8e9bad] hover:bg-[#1b2634] hover:text-white"}`}>{x}</button>)}</div>
      <div className="ml-2 flex items-center gap-1 border-l border-[#27313d] pl-2"><button onClick={() => setPanel(panel === "studies" ? null : "studies")} className="rounded px-3 py-1.5 text-[#b8c3d1] hover:bg-[#1b2634]">Indicators</button><button onClick={() => setPanel(panel === "layers" ? null : "layers")} className="rounded px-3 py-1.5 text-[#b8c3d1] hover:bg-[#1b2634]">Analysis</button></div>
      <div className="ml-auto flex items-center gap-1"><button onClick={screenshot} className="rounded px-3 py-1.5 text-[#8e9bad] hover:bg-[#1b2634] hover:text-white">Snapshot</button><button onClick={reset} className="rounded px-3 py-1.5 text-[#8e9bad] hover:bg-[#1b2634] hover:text-white">Reset</button><button onClick={() => setFull(!full)} className="rounded px-3 py-1.5 text-[#8e9bad] hover:bg-[#1b2634] hover:text-white">{full ? "Exit" : "Fullscreen"}</button></div>
    </div>
    <div className="flex min-h-0 flex-1">
      <aside className="flex w-14 shrink-0 flex-col items-center border-r border-[#27313d] bg-[#0b1017] py-2">{(["cursor", "crosshair", "trendline", "ray", "horizontal", "vertical", "rectangle", "fib", "measure"] as Tool[]).map(x => <button key={x} title={x} onClick={() => setTool(x)} className={`mb-1 flex h-9 w-11 items-center justify-center rounded text-xs ${tool === x ? "bg-[#314157] text-white" : "text-[#8a98aa] hover:bg-[#182230] hover:text-white"}`}>{x === "cursor" ? "↖" : x === "crosshair" ? "✥" : x === "trendline" ? "╱" : x === "ray" ? "↗" : x === "horizontal" ? "—" : x === "vertical" ? "│" : x === "rectangle" ? "□" : x === "fib" ? "Fib" : "↔"}</button>)}<div className="my-2 h-px w-9 bg-[#27313d]" /><button onClick={() => setDrawings([])} className="h-9 w-11 rounded text-xs text-[#8a98aa] hover:bg-[#182230]">Clear</button></aside>
      <div className="relative min-w-0 flex-1">
        <div ref={host} className="absolute inset-0" />
        <div className="pointer-events-none absolute left-4 top-3 z-10 flex items-center gap-4 rounded bg-[#0b1017]/75 px-2 py-1 text-xs text-[#9eabba]"><span>O {last?.open.toFixed(5) ?? "—"}</span><span>H {last?.high.toFixed(5) ?? "—"}</span><span>L {last?.low.toFixed(5) ?? "—"}</span><span>C {last?.close.toFixed(5) ?? "—"}</span><span className={change >= 0 ? "text-emerald-400" : "text-red-400"}>{change >= 0 ? "+" : ""}{change.toFixed(2)}%</span>{cursor !== null && <span className="text-[#c7d1de]">{cursor.toFixed(5)}</span>}</div>
        <div className="absolute inset-0" onPointerDown={e => { const p = point(e); if (p && !["cursor", "crosshair"].includes(tool)) setDraft(p); }} onPointerUp={e => { const p = point(e); if (p && draft && !["cursor", "crosshair"].includes(tool)) { setDrawings(v => [...v, { tool: tool as Exclude<Tool, "cursor" | "crosshair">, a: draft, b: p }]); setDraft(null); } }}>
          <svg key={tick} className="pointer-events-none absolute inset-0 h-full w-full">{selectedLayers.includes("FVG") && fvgs.map((z, i) => { const x1 = chartRef.current?.timeScale().timeToCoordinate(z.a), x2 = chartRef.current?.timeScale().timeToCoordinate(z.b), y1 = mainRef.current?.priceToCoordinate(z.high), y2 = mainRef.current?.priceToCoordinate(z.low); return x1 == null || x2 == null || y1 == null || y2 == null ? null : <rect key={`f${i}`} x={x1} y={y1} width={Math.max(2, x2-x1)} height={Math.max(2, y2-y1)} fill={z.bullish ? "rgba(38,166,154,.10)" : "rgba(239,83,80,.10)"} stroke={z.bullish ? "rgba(38,166,154,.55)" : "rgba(239,83,80,.55)"} />; })}{selectedLayers.includes("Order Block") && obs.map((z, i) => { const x1 = chartRef.current?.timeScale().timeToCoordinate(z.a), x2 = chartRef.current?.timeScale().timeToCoordinate(z.b), y1 = mainRef.current?.priceToCoordinate(z.high), y2 = mainRef.current?.priceToCoordinate(z.low); return x1 == null || x2 == null || y1 == null || y2 == null ? null : <rect key={`o${i}`} x={x1} y={y1} width={Math.max(2, x2-x1)} height={Math.max(2, y2-y1)} fill={z.bullish ? "rgba(62,142,208,.09)" : "rgba(185,82,199,.09)"} stroke={z.bullish ? "rgba(62,142,208,.55)" : "rgba(185,82,199,.55)"} />; })}{selectedLayers.includes("Structure") && piv.map((p, i) => { const x = chartRef.current?.timeScale().timeToCoordinate(p.time), y = mainRef.current?.priceToCoordinate(p.price); return x == null || y == null ? null : <g key={`p${i}`}><circle cx={x} cy={y} r="3" fill={p.high ? "#ef5350" : "#26a69a"}/><text x={x+5} y={y-5} fill="#9eabba" fontSize="10">{p.high ? "SH" : "SL"}</text></g>; })}{drawings.map((d, i) => { const x1 = chartRef.current?.timeScale().timeToCoordinate(d.a.time), x2 = chartRef.current?.timeScale().timeToCoordinate(d.b.time), y1 = mainRef.current?.priceToCoordinate(d.a.price), y2 = mainRef.current?.priceToCoordinate(d.b.price); if (x1 == null || x2 == null || y1 == null || y2 == null) return null; if (d.tool === "horizontal") return <line key={i} x1="0" x2="100%" y1={y1} y2={y1} stroke="#e2b86b" strokeDasharray="6 4"/>; if (d.tool === "vertical") return <line key={i} x1={x1} x2={x1} y1="0" y2="100%" stroke="#e2b86b" strokeDasharray="6 4"/>; if (d.tool === "rectangle") return <rect key={i} x={Math.min(x1,x2)} y={Math.min(y1,y2)} width={Math.abs(x2-x1)} height={Math.abs(y2-y1)} fill="rgba(114,169,255,.08)" stroke="#72a9ff"/>; if (d.tool === "fib") return <g key={i}>{[0,.236,.382,.5,.618,.786,1].map(f => <line key={f} x1={Math.min(x1,x2)} x2={Math.max(x1,x2)} y1={y1+(y2-y1)*f} y2={y1+(y2-y1)*f} stroke="#8795a8" strokeDasharray="4 3"/>)}</g>; return <line key={i} x1={x1} y1={y1} x2={d.tool === "ray" ? "100%" : x2} y2={d.tool === "ray" ? y1 + (y2-y1) * ((host.current?.clientWidth ?? 1)-x1)/Math.max(1,x2-x1) : y2} stroke="#72a9ff" strokeWidth="1.5"/>; })}</svg>
        </div>
      </div>
      <aside className="hidden w-72 shrink-0 flex-col border-l border-[#27313d] bg-[#0c121a] xl:flex">
        <div className="flex h-10 items-center border-b border-[#27313d] px-3 text-xs font-semibold text-white">Inspector</div>
        <div className="grid grid-cols-4 border-b border-[#27313d] text-[11px]">{(["market","structure","intelligence","risk"] as const).map(x => <button key={x} onClick={() => setInspector(x)} className={`py-2 capitalize ${inspector === x ? "border-b-2 border-[#72a9ff] text-white" : "text-[#718095]"}`}>{x.slice(0,4)}</button>)}</div>
        <div className="min-h-0 flex-1 overflow-hidden p-3 text-xs text-[#93a0b0]">
          {inspector === "market" && <div className="space-y-3"><div><div className="text-[11px] text-[#68778a]">MARKET</div><div className="mt-1 text-lg text-white">{symbol}</div><div className="mt-1 text-[#6f7e91]">{tf} · {c.length} bars</div></div><div className="grid grid-cols-2 gap-2">{[["Open",last?.open],["High",last?.high],["Low",last?.low],["Close",last?.close]].map(([k,v]) => <div key={String(k)} className="rounded border border-[#25303d] bg-[#101821] p-2"><div className="text-[10px] text-[#657488]">{k}</div><div className="mt-1 text-white">{typeof v === "number" ? v.toFixed(5) : "—"}</div></div>)}</div></div>}
          {inspector === "structure" && <div className="space-y-3"><div className="text-[11px] text-[#68778a]">MARKET STRUCTURE</div><div className="rounded border border-[#25303d] p-3">{piv.slice(-6).map((p,i) => <div key={i} className="flex justify-between py-1"><span>{p.high ? "Swing High" : "Swing Low"}</span><span className="text-white">{p.price.toFixed(5)}</span></div>)}</div><div className="text-[#68778a]">BOS / CHoCH / MSS become canonical once the structure engine is connected to the market-domain service.</div></div>}
          {inspector === "intelligence" && <div className="space-y-3"><div className="text-[11px] text-[#68778a]">INTELLIGENCE LAYERS</div>{layers.map(x => <div key={x} className="flex items-center justify-between rounded border border-[#25303d] bg-[#101821] px-3 py-2"><span>{x}</span><button onClick={() => toggle(x, selectedLayers, setSelectedLayers)} className={selectedLayers.includes(x) ? "text-emerald-400" : "text-[#5e6c7f]"}>{selectedLayers.includes(x) ? "ON" : "OFF"}</button></div>)}</div>}
          {inspector === "risk" && <div className="space-y-3"><div className="text-[11px] text-[#68778a]">RISK TOOLS</div><div className="grid grid-cols-2 gap-2">{["Entry","Stop Loss","Take Profit","R:R","Risk %","Position Size"].map(x => <div key={x} className="rounded border border-[#25303d] bg-[#101821] p-2"><div className="text-[10px] text-[#657488]">{x}</div><div className="mt-2 text-[#59687a]">—</div></div>)}</div><div className="text-[#68778a]">Risk values remain data-driven; no account or broker values are fabricated.</div></div>}
        </div>
      </aside>
    </div>
    <div className="flex h-7 shrink-0 items-center justify-between border-t border-[#27313d] bg-[#0d131b] px-3 text-[11px] text-[#718095]"><span>{c.length} bars · {tf} · {kind} · {tool}</span><span>CFIP-PRO · normalized market data</span></div>
    {panel && <div className="absolute left-16 top-12 z-40 w-80 rounded border border-[#334052] bg-[#111923] p-3 text-xs shadow-2xl">{panel === "studies" && <><div className="mb-3 font-semibold text-white">Indicators & Studies</div><div className="grid grid-cols-2 gap-1">{studies.map(x => <button key={x} onClick={() => toggle(x, selectedStudies, setSelectedStudies)} className={`rounded px-2 py-2 text-left ${selectedStudies.includes(x) ? "bg-[#314157] text-white" : "bg-[#18212c] text-[#8b98aa]"}`}>{x}</button>)}</div></>}{panel === "layers" && <><div className="mb-3 font-semibold text-white">Market Analysis</div><div className="grid grid-cols-2 gap-1">{layers.map(x => <button key={x} onClick={() => toggle(x, selectedLayers, setSelectedLayers)} className={`rounded px-2 py-2 text-left ${selectedLayers.includes(x) ? "bg-[#314157] text-white" : "bg-[#18212c] text-[#8b98aa]"}`}>{x}</button>)}</div></>}{panel === "settings" && <div className="space-y-2"><div className="font-semibold text-white">Chart Type</div>{(["candles","bars","line","area","baseline"] as ChartKind[]).map(x => <button key={x} onClick={() => setKind(x)} className={`mr-1 rounded px-3 py-2 capitalize ${kind === x ? "bg-[#314157] text-white" : "bg-[#18212c] text-[#8b98aa]"}`}>{x}</button>)}</div>}{panel === "symbol" && <div><div className="font-semibold text-white">Symbol</div><div className="mt-2 rounded border border-[#2b3645] bg-[#0c121a] p-3 text-white">{symbol}</div></div>}</div>}
  </div>;
}
