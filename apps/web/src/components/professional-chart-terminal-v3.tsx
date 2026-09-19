"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { ColorType, CrosshairMode, createChart, type IChartApi, type ISeriesApi, type SeriesType, type UTCTimestamp } from "lightweight-charts";
import type { MarketObservation } from "@/lib/api";
import { getMarketObservations, postUnifiedAnalysis, type UnifiedAnalysisRead } from "@/lib/api";
import { TerminalSidebar } from "@/components/terminal/terminal-sidebar";
import { SymbolPicker } from "@/components/terminal/symbol-picker";
import { ChartAttribution } from "@/components/terminal/chart-attribution";
import { forexSymbols } from "@/components/terminal/symbols";
import { t, localeNames, rtlLocales } from "@/components/terminal/i18n";
import { DEFAULT_PREFERENCES, type ChartKind, type ChartPreferences, type Drawing, type InspectorTab, type Locale, type Point, type Timeframe, type Tool } from "@/components/terminal/types";
import type { UnifiedAnalysis } from "@/components/terminal/analysis-contracts";
import "./terminal/terminal-theme.module.css";
import { toCandles, fvg, pivots, supportResistance, sessionRange, marketStructure, orderBlocks } from "@/components/terminal/chart-math";
import { addMainSeries, addVolumeSeries, setMainSeriesData } from "@/components/terminal/chart-engine";
import { renderRegisteredIndicators } from "@/components/terminal/indicator-renderer";
import { computeAnalysisSnapshot } from "@/components/terminal/analysis-engine";
import { createReplayState, replayPause, replayPlay, replayReset, replaySetSpeed, replaySlice, replayStep, type ReplayState } from "@/components/terminal/replay-engine";
import { loadTerminalSession, saveTerminalSession } from "@/components/terminal/session-storage";
import { TIMEFRAMES, INDICATORS, DRAWING_TOOLS, TOOL_GLYPHS, CHART_KINDS } from "@/components/terminal/terminal-config";



export function ProfessionalChartTerminalV3({ observations: initial, symbol: initialSymbol }: { observations: MarketObservation[]; symbol: string }) {
  const host=useRef<HTMLDivElement>(null);
  const marketVersionRef=useRef(`${initial.length}:${initial.at(-1)?.observed_at ?? ""}:${initial.at(-1)?.last ?? ""}`);
  const chartRef=useRef<IChartApi|null>(null);
  const mainRef=useRef<ISeriesApi<SeriesType>|null>(null);
  const [symbol,setSymbol]=useState(initialSymbol),[rows,setRows]=useState(initial),[tf,setTf]=useState<Timeframe>("1m"),[kind,setKind]=useState<ChartKind>("candles"),[locale,setLocale]=useState<Locale>("en"),[sidebar,setSidebar]=useState(DEFAULT_PREFERENCES.rightSidebar),[rail,setRail]=useState(DEFAULT_PREFERENCES.leftRail),[tab,setTab]=useState<InspectorTab>("market"),[panel,setPanel]=useState<string|null>(null),[tool,setTool]=useState<Tool>("cursor"),[selected,setSelected]=useState<string[]>(["EMA20"]),[prefs,setPrefs]=useState<ChartPreferences>(DEFAULT_PREFERENCES),[drawings,setDrawings]=useState<Drawing[]>([]),[pendingPoint,setPendingPoint]=useState<Drawing["a"]|null>(null),[selectedDrawingId,setSelectedDrawingId]=useState<string|null>(null),[sessionReady,setSessionReady]=useState(false),[live,setLive]=useState(false),[error,setError]=useState(false),[backendAnalysis,setBackendAnalysis]=useState<UnifiedAnalysisRead|null>(null),[overlayTick,setOverlayTick]=useState(0);
  const drawingDragRef=useRef<{id:string;origin:Drawing;before:Drawing[];startTime:number;startPrice:number}|null>(null);
  const drawingsRef=useRef<Drawing[]>(drawings);
  drawingsRef.current=drawings;
  const drawingHistoryRef=useRef<Drawing[][]>([]);
  const drawingRedoRef=useRef<Drawing[][]>([]);

  const liveCandles=useMemo(()=>toCandles(rows,tf),[rows,tf]);
  const [replay,setReplay]=useState<ReplayState>(()=>createReplayState(0));
  const candles=useMemo(
    ()=>replay.status==="idle" ? liveCandles : replaySlice(liveCandles,replay),
    [liveCandles,replay],
  );

  useEffect(() => {
    if (replay.status === "idle") {
      setReplay(createReplayState(liveCandles.length));
      return;
    }
    setReplay(current => ({
      ...current,
      end: Math.max(current.start, liveCandles.length - 1),
      cursor: Math.min(current.cursor, Math.max(current.start, liveCandles.length - 1)),
    }));
  }, [liveCandles.length]);

  useEffect(() => {
    if (replay.status !== "playing") return;
    const interval = window.setInterval(() => {
      setReplay(current => replayStep(current, 1));
    }, Math.max(50, 500 / replay.speed));
    return () => window.clearInterval(interval);
  }, [replay.status, replay.speed]);
  const last=candles.at(-1),prev=candles.at(-2);
  const zones=useMemo(()=>fvg(candles),[candles]);
  const pivotPoints=useMemo(()=>pivots(candles),[candles]);
  const levels=useMemo(()=>supportResistance(candles),[candles]);
  const session=useMemo(()=>sessionRange(candles),[candles]);
  const structure=useMemo(()=>marketStructure(candles),[candles]);
  const blocks=useMemo(()=>orderBlocks(candles),[candles]);
  const analysisSnapshot = useMemo(() => computeAnalysisSnapshot(candles, tf), [candles, tf]);
  const analysisCandles = candles.length > 1 ? candles.slice(0, -1) : candles;
  const analysis = analysisSnapshot.analysis;
  const closedBarKey = analysisCandles.at(-1)?.time ?? null;
  useEffect(() => {
    if (analysisCandles.length < 5) {
      setBackendAnalysis(null);
      return;
    }
    let active = true;
    const run = async () => {
      try {
        const result = await postUnifiedAnalysis(symbol, tf, analysisCandles);
        if (active) setBackendAnalysis(result);
      } catch {
        if (active) setBackendAnalysis(null);
      }
    };
    void run();
    return () => { active = false; };
  }, [closedBarKey, symbol, tf]);

  const canonicalAnalysis: UnifiedAnalysis = backendAnalysis
    ? {
        ...analysis,
        bias: backendAnalysis.bias,
        score: backendAnalysis.score,
        confidence: backendAnalysis.confidence,
        regime: backendAnalysis.regime,
        recommendation: backendAnalysis.recommendation,
        evidence: backendAnalysis.evidence,
        confluence: {
          score: backendAnalysis.confluence_score,
          threshold: backendAnalysis.confluence_threshold,
          accepted: backendAnalysis.confluence_accepted,
          gates: backendAnalysis.gates.filter((gate): gate is UnifiedAnalysis["confluence"]["gates"][number] =>
            ["htf_alignment","liquidity_or_fvg","zone_or_premium","displacement_or_structure"].includes(gate.id),
          ).map(gate => ({ id: gate.id, passed: gate.passed, detail: gate.detail })),
        },
      }
    : analysis;
  const pct=last&&prev?((last.close-prev.close)/prev.close)*100:0;
  const meta=forexSymbols.find(x=>x.symbol===symbol);

  useEffect(() => {
    const session = loadTerminalSession({
      symbol: initialSymbol,
      timeframe: "1m",
      chartKind: "candles",
      locale: "en",
      tool: "cursor",
      selectedStudies: ["EMA20"],
      preferences: DEFAULT_PREFERENCES,
      drawings: [],
    });
    setSymbol(session.symbol);
    setTf(session.timeframe);
    setKind(session.chartKind);
    setLocale(session.locale);
    setTool(session.tool);
    setSelected(session.selectedStudies);
    setPrefs(session.preferences);
    setSidebar(session.preferences.rightSidebar);
    setRail(session.preferences.leftRail);
    setDrawings(session.drawings);
    setSessionReady(true);
  }, [initialSymbol]);

  useEffect(() => {
    if (!sessionReady) return;
    saveTerminalSession({
      symbol, timeframe: tf, chartKind: kind, locale, tool,
      selectedStudies: selected, preferences: { ...prefs, rightSidebar: sidebar, leftRail: rail }, drawings,
    });
  }, [sessionReady, symbol, tf, kind, locale, tool, selected, prefs, sidebar, rail, drawings]);

  const resetView=()=>chartRef.current?.timeScale().fitContent();
  const updateDrawings=(next: Drawing[] | ((current: Drawing[]) => Drawing[]))=>{
    setDrawings(current=>{
      const resolved=typeof next==="function" ? next(current) : next;
      if(JSON.stringify(resolved)!==JSON.stringify(current)){
        drawingHistoryRef.current=[...drawingHistoryRef.current.slice(-49),current];
        drawingRedoRef.current=[];
      }
      return resolved;
    });
  };
  const undoDrawing=()=>{
    setDrawings(current=>{
      const previous=drawingHistoryRef.current.pop();
      if(!previous)return current;
      drawingRedoRef.current=[...drawingRedoRef.current,current];
      setSelectedDrawingId(null);
      return previous;
    });
  };
  const redoDrawing=()=>{
    setDrawings(current=>{
      const next=drawingRedoRef.current.pop();
      if(!next)return current;
      drawingHistoryRef.current=[...drawingHistoryRef.current,current];
      return next;
    });
  };
  const toggleFullscreen=async()=>{
    const element=host.current?.parentElement;
    if(!element)return;
    if(document.fullscreenElement)await document.exitFullscreen();
    else await element.requestFullscreen();
  };

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) return;
      if (event.key === "Escape") { setPanel(null); setTool("cursor"); setPendingPoint(null); setSelectedDrawingId(null); return; }
      if ((event.ctrlKey||event.metaKey) && event.key.toLowerCase()==="z") { event.preventDefault(); if(event.shiftKey) redoDrawing(); else undoDrawing(); return; }
      if ((event.ctrlKey||event.metaKey) && event.key.toLowerCase()==="y") { event.preventDefault(); redoDrawing(); return; }
      if ((event.key==="Delete"||event.key==="Backspace") && selectedDrawingId) { updateDrawings(current=>current.filter(d=>d.id!==selectedDrawingId)); setSelectedDrawingId(null); return; }
      if (event.key === "f" || event.key === "F") { void toggleFullscreen(); return; }
      if (event.key === "r" || event.key === "R") { resetView(); return; }
      if (event.key === "1") setTf("1m");
      if (event.key === "2") setTf("5m");
      if (event.key === "3") setTf("15m");
      if (event.key === "4") setTf("30m");
      if (event.key === "5") setTf("1H");
      if (event.key === "6") setTf("4H");
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);
  
  useEffect(()=>{
    if(!panel)return;
    const close=(event:PointerEvent)=>{
      const target=event.target as HTMLElement;
      if(!target.closest("[data-terminal-panel]") && !target.closest("[data-terminal-trigger]")) setPanel(null);
    };
    window.addEventListener("pointerdown",close);
    return()=>window.removeEventListener("pointerdown",close);
  },[panel]);

  useEffect(()=>{
    let active=true;
    const load=async()=>{
      try{
        const next=await getMarketObservations(symbol,"reference",1000);
        if(active){
          const version = `${next.length}:${next.at(-1)?.observed_at ?? ""}:${next.at(-1)?.last ?? ""}`;
          if (version !== marketVersionRef.current) { marketVersionRef.current = version; setRows(next); }
          setLive(next.length>0);setError(false);
        }
      }catch{
        if(active){setLive(false);setError(true);}
      }
    };
    void load();
    const id=window.setInterval(load,2000);
    return()=>{active=false;window.clearInterval(id);};
  },[symbol]);

  useEffect(()=>{
    if(!host.current)return;
    const c=createChart(host.current,{
      autoSize:true,
      layout:{background:{type:ColorType.Solid,color:"#080b10"},textColor:"#b7c1ce",fontSize:13,fontFamily:"Inter,Segoe UI,Arial,sans-serif",attributionLogo:true},
      grid:{vertLines:{color:prefs.showGrid?"#141b25":"transparent"},horzLines:{color:prefs.showGrid?"#141b25":"transparent"}},
      rightPriceScale:{borderColor:"#2a3442",autoScale:true,alignLabels:true,minimumWidth:86},
      timeScale:{borderColor:"#2a3442",timeVisible:true,secondsVisible:false,rightOffset:10,barSpacing:9,minBarSpacing:2,maxBarSpacing:30},
      crosshair:{mode:CrosshairMode.Normal,vertLine:{color:"#66758a",width:1,style:3,labelBackgroundColor:"#354458"},horzLine:{color:"#66758a",width:1,style:3,labelBackgroundColor:"#354458"}},
      handleScroll:{mouseWheel:true,pressedMouseMove:true,horzTouchDrag:true,vertTouchDrag:true},
      handleScale:{mouseWheel:true,pinch:true,axisPressedMouseMove:true,axisDoubleClickReset:true},
    });
    chartRef.current=c;
    return()=>{c.remove();chartRef.current=null;};
  },[]);

  useEffect(() => {
    chartRef.current?.applyOptions({
      grid: {
        vertLines: { color: prefs.showGrid ? "#141b25" : "transparent" },
        horzLines: { color: prefs.showGrid ? "#141b25" : "transparent" },
      },
    });
  }, [prefs.showGrid]);

  useEffect(()=>{
    const c=chartRef.current;
    if(!c||!candles.length)return;
    const visibleRange = c.timeScale().getVisibleLogicalRange();
    for(const pane of c.panes())for(const s of pane.getSeries())c.removeSeries(s);
    while (c.panes().length > 1) c.removePane(c.panes().length - 1);
    const hasOscillatorPane = selected.some(x => ["RSI14","MACD","DMI14","STOCH14"].includes(x));
    const hasVolumePane = prefs.showVolume;
    if (hasOscillatorPane) c.addPane(true);
    if (hasVolumePane) c.addPane(true);

    const main=addMainSeries(c,kind,candles[0].close);
    setMainSeriesData(main,kind,candles);
    mainRef.current=main;

    renderRegisteredIndicators(c, candles, selected, 1);
    if (hasOscillatorPane) c.panes()[1].setHeight(170);
    if (hasVolumePane) addVolumeSeries(c,candles,hasOscillatorPane ? 2 : 1);
    if (hasVolumePane) c.panes()[hasOscillatorPane ? 2 : 1].setHeight(110);
    if (visibleRange) c.timeScale().setVisibleLogicalRange(visibleRange); else c.timeScale().fitContent();
    setOverlayTick(v => v + 1);
  },[candles,kind,selected,prefs.showVolume]);

  const toggleSidebar=(value:boolean)=>{setSidebar(value);setPrefs(p=>({...p,rightSidebar:value}));};
  const toggleRail=(value:boolean)=>{setRail(value);setPrefs(p=>({...p,leftRail:value}));};

  const tt=(x:Tool)=>({
    cursor:t("en","cursor"),crosshair:t(locale,"crosshair"),trendline:t(locale,"trendline"),ray:t(locale,"ray"),
    horizontal:t(locale,"horizontal"),vertical:t(locale,"vertical"),rectangle:t(locale,"rectangle"),fib:t(locale,"fibonacci"),
    measure:t(locale,"measure"),long:t(locale,"longPosition"),short:t(locale,"shortPosition")
  }[x]);

  const toggle=(id:string)=>setSelected(s=>s.includes(id)?s.filter(x=>x!==id):[...s,id]);\n\n  // Terminal/chart presentation is intentionally EN/LTR and is not mutated by app-language selection.\n  // Locale is persisted as application preference for the surrounding product surfaces.\n

  useEffect(() => {
    const c = chartRef.current;
    if (!c) return;
    const redraw = () => setOverlayTick(v => v + 1);
    const scale = c.timeScale();
    scale.subscribeVisibleLogicalRangeChange(redraw);
    scale.subscribeSizeChange(redraw);
    window.addEventListener("resize", redraw);
    return () => {
      scale.unsubscribeVisibleLogicalRangeChange(redraw);
      scale.unsubscribeSizeChange(redraw);
      window.removeEventListener("resize", redraw);
    };
  }, []);

  useEffect(()=>{
    const move=(event:PointerEvent)=>{
      const drag=drawingDragRef.current;
      const c=chartRef.current, main=mainRef.current, section=host.current?.parentElement;
      if(!drag||!c||!main||!section)return;
      const rect=section.getBoundingClientRect();
      const time=c.timeScale().coordinateToTime(event.clientX-rect.left);
      const price=main.coordinateToPrice(event.clientY-rect.top);
      if(typeof time!=="number"||price===null)return;
      const dt=time-drag.startTime, dp=price-drag.startPrice;
      setDrawings(current=>current.map(d=>d.id===drag.id?{
        ...d,
        a:{...drag.origin.a,time:(drag.origin.a.time as number)+dt,price:drag.origin.a.price+dp},
        b:{...drag.origin.b,time:(drag.origin.b.time as number)+dt,price:drag.origin.b.price+dp}
      }:d));
      setOverlayTick(v=>v+1);
    };
    const up=()=>{const drag=drawingDragRef.current;if(drag&&JSON.stringify(drawingsRef.current)!==JSON.stringify(drag.before)){drawingHistoryRef.current=[...drawingHistoryRef.current.slice(-49),drag.before];drawingRedoRef.current=[];} drawingDragRef.current=null;};
    window.addEventListener("pointermove",move);
    window.addEventListener("pointerup",up);
    return()=>{window.removeEventListener("pointermove",move);window.removeEventListener("pointerup",up);};
  },[]);

  const startDrawingDrag=(event:React.PointerEvent<SVGElement>,drawing:Drawing)=>{
    event.stopPropagation();
    if(drawing.locked)return;
    const c=chartRef.current,main=mainRef.current,section=host.current?.parentElement;
    if(!c||!main||!section)return;
    const rect=section.getBoundingClientRect();
    const time=c.timeScale().coordinateToTime(event.clientX-rect.left);
    const price=main.coordinateToPrice(event.clientY-rect.top);
    if(typeof time!=="number"||price===null)return;
    setSelectedDrawingId(drawing.id);
    drawingDragRef.current={id:drawing.id,origin:drawing,before:drawingsRef.current,startTime:time,startPrice:price};
  };

  const placeDrawing=(event: React.MouseEvent<HTMLElement>)=>{
    if(tool==="cursor"||tool==="crosshair"||!chartRef.current||!mainRef.current)return;
    const rect=event.currentTarget.getBoundingClientRect();
    const x=event.clientX-rect.left, y=event.clientY-rect.top;
    const time=chartRef.current.timeScale().coordinateToTime(x);
    const price=mainRef.current.coordinateToPrice(y);
    if(time===null||price===null)return;
    if(typeof time !== "number")return;
    const point:Point={time:time as UTCTimestamp,price:price as number};
    if(!pendingPoint){setPendingPoint(point);return;}
    const drawing:Drawing={id:crypto.randomUUID(),tool:tool as Drawing["tool"],a:pendingPoint,b:point,visible:true,locked:false};
    updateDrawings(value=>[...value,drawing]);
    setPendingPoint(null);
  };

  return <div dir="ltr" data-terminal-locale="en" className="cfip-terminal relative flex h-full min-h-0 flex-col bg-[#080b10] text-[#d8e0ea]">
    <header className="cfip-terminal-topbar relative flex h-12 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-2">
      <button onClick={()=>toggleRail(!rail)} title={rail?t(locale,"hideRail"):t(locale,"showRail")} className="mr-2 rounded border border-[#334155] px-2 py-1.5 text-xs">☰</button>
      <button data-terminal-trigger onClick={()=>setPanel(panel==="symbol"?null:"symbol")} className="flex min-w-[180px] items-center gap-2 rounded px-2 py-1.5 text-left hover:bg-[#17202c]"><strong className="text-[15px]">{symbol}</strong><span className="text-[10px] text-[#66758a]">{meta?.name??"Forex"}</span></button>
      {panel==="symbol"&&<div data-terminal-panel className="absolute left-2 top-11 z-50"><SymbolPicker locale="en" value={symbol} onChange={s=>{setSymbol(s.symbol);setPanel(null)}}/></div>}
      <span className="mx-2 h-5 w-px bg-[#293342]"/>
      <div className="flex gap-1">{TIMEFRAMES.map(x=><button key={x} onClick={()=>setTf(x)} className={`rounded px-2.5 py-1.5 text-xs ${tf===x?"bg-[#23364d] text-white":"text-[#8391a4] hover:bg-[#17202c]"}`}>{x}</button>)}<button data-terminal-trigger onClick={()=>setPanel(panel==="timeframe"?null:"timeframe")} className="rounded px-2 text-[#8391a4]">⋯</button></div>
      <div className="ml-auto flex items-center gap-1"><button onClick={undoDrawing} title="Ctrl/Cmd+Z" className="rounded px-2 py-1.5 text-xs hover:bg-[#17202c]">↶</button><button onClick={redoDrawing} title="Ctrl/Cmd+Y" className="rounded px-2 py-1.5 text-xs hover:bg-[#17202c]">↷</button>
        <button onClick={resetView} title="R" className="rounded px-2.5 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"autoFit")}</button>
        <button onClick={()=>{const c=chartRef.current;if(!c)return;const canvas=c.takeScreenshot();const link=document.createElement("a");link.download=`cfip-${symbol.replace("/","-")}-${tf}.png`;link.href=canvas.toDataURL("image/png");link.click();}} className="rounded px-2.5 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"screenshot")}</button>
        <button onClick={toggleFullscreen} className="rounded px-2.5 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"fullscreen")}</button>
        <button data-terminal-trigger onClick={()=>setPanel(panel==="replay"?null:"replay")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"replay")}</button>
        <button data-terminal-trigger onClick={()=>setPanel(panel==="indicators"?null:"indicators")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"indicators")}</button>
        <button data-terminal-trigger onClick={()=>setPanel(panel==="chartType"?null:"chartType")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"chartType")}</button>
        <button data-terminal-trigger onClick={()=>setPanel(panel==="language"?null:"language")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{locale.toUpperCase()}</button>
        <button data-terminal-trigger onClick={()=>setPanel(panel==="settings"?null:"settings")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"settings")}</button>
      </div>
      {panel==="replay"&&<div data-terminal-panel className="absolute right-72 top-11 z-50 w-72 rounded-lg border border-[#334155] bg-[#0d131b] p-3 shadow-2xl">
        <div className="mb-2 flex items-center justify-between text-xs"><span className="font-medium text-white">{t(locale,"replay")}</span><span className="tabular-nums text-[#64748b]">{replay.cursor + 1} / {Math.max(1,replay.end - replay.start + 1)}</span></div>
        <div className="flex gap-1">
          <button onClick={()=>setReplay(replay.status==="playing"?replayPause(replay):replayPlay(replay))} className="rounded bg-[#20354b] px-3 py-2 text-xs text-white">{replay.status==="playing"?"Pause":"Play"}</button>
          <button onClick={()=>setReplay(replayStep(replay,1))} className="rounded border border-[#334155] px-3 py-2 text-xs">Step</button>
          <button onClick={()=>setReplay(replayReset(replay))} className="rounded border border-[#334155] px-3 py-2 text-xs">Reset</button>
        </div>
        <div className="mt-3 flex flex-wrap gap-1">
          {[0.5,1,2,4,8].map(speed=><button key={speed} onClick={()=>setReplay(replaySetSpeed(replay,speed))} className={`rounded px-2 py-1 text-[10px] ${replay.speed===speed?"bg-[#23364d] text-white":"text-[#8391a4] hover:bg-[#17202c]"}`}>{speed}×</button>)}
        </div>
        <div className="mt-2 text-[10px] text-[#64748b]">Historical replay freezes the chart cursor while the live feed continues in the background.</div>
      </div>}
      {panel==="timeframe"&&<div data-terminal-panel className="absolute right-52 top-11 z-50 grid w-60 grid-cols-3 gap-1 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{TIMEFRAMES.map(x=><button key={x} onClick={()=>{setTf(x);setPanel(null)}} className="rounded px-2 py-2 text-xs hover:bg-[#17202c]">{x}</button>)}</div>}
      {panel==="chartType"&&<div data-terminal-panel className="absolute right-40 top-11 z-50 w-44 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{CHART_KINDS.map(x=><button key={x} onClick={()=>{setKind(x);setPanel(null)}} className="block w-full rounded px-3 py-2 text-left text-xs hover:bg-[#17202c]">{t(locale,x==="candles"?"candlestick":x)}</button>)}</div>}
      {panel==="indicators"&&<div data-terminal-panel className="absolute right-28 top-11 z-50 grid w-64 grid-cols-2 gap-1 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{INDICATORS.map(x=><button key={x} onClick={()=>toggle(x)} className={`rounded px-3 py-2 text-left text-xs ${selected.includes(x)?"bg-[#20354b] text-white":"hover:bg-[#17202c]"}`}>{x}</button>)}</div>}
      {panel==="language"&&<div data-terminal-panel className="absolute right-2 top-11 z-50 grid w-64 grid-cols-2 gap-1 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{(Object.keys(localeNames) as Locale[]).map(x=><button key={x} onClick={()=>{setLocale(x);setPanel(null)}} className="rounded px-3 py-2 text-left text-xs hover:bg-[#17202c]">{localeNames[x]}</button>)}</div>}
      {panel==="settings"&&<div data-terminal-panel className="absolute right-2 top-11 z-50 w-72 rounded-lg border border-[#334155] bg-[#0d131b] p-3 shadow-2xl">
        {([
          ["showGrid","grid"],["showVolume","volume"],["showSessions","sessions"],["showBidAsk","bidAsk"],["magnet","magnet"]
        ] as const).map(([key,label])=><label key={key} className="flex items-center justify-between border-b border-[#1f2937] px-2 py-2.5 text-xs last:border-0"><span>{t(locale,label)}</span><input type="checkbox" checked={prefs[key]} onChange={e=>setPrefs({...prefs,[key]:e.target.checked})}/></label>)}
      </div>}
    </header>
    <div className="flex min-h-0 flex-1">
      {rail&&<nav className="cfip-terminal-rail flex w-12 shrink-0 flex-col items-center gap-1 border-r border-[#27313d] bg-[#0b1017] py-2">{DRAWING_TOOLS.map(x=><button key={x} onClick={()=>setTool(x)} title={tt(x)} aria-label={tt(x)} className={`h-9 w-9 rounded text-xs ${tool===x?"bg-[#20354b] text-white":"text-[#8290a3] hover:bg-[#17202c]"}`}>{TOOL_GLYPHS[x]}</button>)}</nav>}
      <section className="relative min-w-0 flex-1" onClick={placeDrawing}>
<div className="absolute left-3 top-8 z-20 flex gap-2 text-[10px] text-[#66758a]">
          {pivotPoints.at(-1) && <span>Structure: {pivotPoints.at(-1)?.high ? "swing high" : "swing low"}</span>}
          {zones.length > 0 && <span>FVG {zones.filter(z=>z.bullish).length}↑ / {zones.filter(z=>!z.bullish).length}↓</span>}
        </div>        <div className="absolute left-3 top-2 z-20 flex items-center gap-3 text-xs">
          <strong className="text-white">{symbol}</strong><span className="text-[#8492a5]">{tf}</span>
          {last&&<><span>O {last.open.toFixed(meta?.digits??5)}</span><span>H {last.high.toFixed(meta?.digits??5)}</span><span>L {last.low.toFixed(meta?.digits??5)}</span><span>C {last.close.toFixed(meta?.digits??5)}</span><span className={pct>=0?"text-emerald-400":"text-red-400"}>{pct>=0?"+":""}{pct.toFixed(2)}%</span></>}
          <span className={live?"text-emerald-400":"text-amber-400"}>● {live?t(locale,"live"):error?t(locale,"dataOffline"):t(locale,"loading")}</span>
        </div>
        {zones.length > 0 && <div className="pointer-events-none absolute left-3 bottom-10 z-10 rounded border border-[#334155] bg-[#0d131b]/85 px-2 py-1 text-[10px] text-[#94a3b8]">{zones.length} FVG zones</div>}
        {levels.length > 0 && <div className="pointer-events-none absolute right-3 bottom-10 z-10 rounded border border-[#334155] bg-[#0d131b]/85 px-2 py-1 text-[10px] text-[#94a3b8]">{levels.length} S/R levels</div>}
        {session && prefs.showSessions && <div className="pointer-events-none absolute left-3 bottom-20 z-10 rounded border border-[#334155] bg-[#0d131b]/85 px-2 py-1 text-[10px] text-[#94a3b8]">Session {session.low.toFixed(meta?.digits??5)} — {session.high.toFixed(meta?.digits??5)}</div>}
        {prefs.showBidAsk && last && (last.bid || last.ask) && <div className="pointer-events-none absolute right-3 top-8 z-20 rounded border border-[#334155] bg-[#0d131b]/90 px-2 py-1 text-[10px] tabular-nums text-[#c8d2df]">{last.bid && <span className="mr-3 text-[#60a5fa]">B {Number(last.bid).toFixed(meta?.digits??5)}</span>}{last.ask && <span className="text-[#f59e0b]">A {Number(last.ask).toFixed(meta?.digits??5)}</span>}</div>}
        <svg key={overlayTick} aria-label="Chart drawings" className="pointer-events-none absolute inset-0 z-10 h-full w-full overflow-visible">
          {zones.map((z,i)=>{
            const xa=chartRef.current?.timeScale().timeToCoordinate(z.a), xb=chartRef.current?.timeScale().timeToCoordinate(z.b);
            const ya=mainRef.current?.priceToCoordinate(z.high), yb=mainRef.current?.priceToCoordinate(z.low);
            if(xa===null||xa===undefined||xb===null||xb===undefined||ya===null||ya===undefined||yb===null||yb===undefined)return null;
            return <rect key={'fvg-'+i} x={Math.min(xa,xb)} y={Math.min(ya,yb)} width={Math.max(1,Math.abs(xb-xa))} height={Math.max(1,Math.abs(yb-ya))} fill={z.bullish?'rgba(34,197,94,.07)':'rgba(239,68,68,.07)'} stroke={z.bullish?'#22c55e':'#ef4444'} strokeWidth='1' strokeDasharray='4 3'/>;
          })}
          {drawings.filter(d=>d.visible!==false).map(d=>{
            const x1=chartRef.current?.timeScale().timeToCoordinate(d.a.time), x2=chartRef.current?.timeScale().timeToCoordinate(d.b.time);
            const y1=mainRef.current?.priceToCoordinate(d.a.price), y2=mainRef.current?.priceToCoordinate(d.b.price);
            if(x1===null||x1===undefined||x2===null||x2===undefined||y1===null||y1===undefined||y2===null||y2===undefined)return null;
            const selectedStroke=selectedDrawingId===d.id?"#fbbf24":"#94a3b8";
            const hit=(xA:number,yA:number,xB:number,yB:number)=><line x1={xA} y1={yA} x2={xB} y2={yB} stroke="transparent" strokeWidth="16" pointerEvents="stroke" onPointerDown={e=>startDrawingDrag(e,d)}/>;
            if(d.tool==="horizontal") return <g key={d.id}>{hit(0,y1,1000,y1)}<line x1={0} x2="100%" y1={y1} y2={y1} stroke={selectedStroke} strokeWidth={selectedDrawingId===d.id?2:1} strokeDasharray="5 4"/></g>;
            if(d.tool==="vertical") return <g key={d.id}>{hit(x1,0,x1,1000)}<line x1={x1} x2={x1} y1={0} y2="100%" stroke={selectedStroke} strokeWidth={selectedDrawingId===d.id?2:1} strokeDasharray="5 4"/></g>;
            if(d.tool==="rectangle") return <g key={d.id}>{hit(x1,y1,x2,y2)}<rect x={Math.min(x1,x2)} y={Math.min(y1,y2)} width={Math.abs(x2-x1)} height={Math.abs(y2-y1)} fill="rgba(112,167,255,.08)" stroke={selectedDrawingId===d.id?"#fbbf24":"#70a7ff"} strokeWidth={selectedDrawingId===d.id?2:1}/></g>;
            if(d.tool==="fib"){const levels=[0,.236,.382,.5,.618,.786,1];return <g key={d.id}>{hit(x1,y1,x2,y2)}{levels.map(level=>{const yy=y1+(y2-y1)*level;return <g key={level}><line x1={0} x2="100%" y1={yy} y2={yy} stroke="#fbbf24" strokeWidth={selectedDrawingId===d.id?2:1} strokeDasharray="3 3"/><text x={Math.max(x1,x2)+6} y={yy-3} fill="#d8e0ea" fontSize="9">{(level*100).toFixed(1)}% · {((d.a.price+(d.b.price-d.a.price)*level)).toFixed(meta?.digits??5)}</text></g>})}</g>};
            const stroke=d.tool==="short"?"#ef5350":d.tool==="long"?"#22c55e":"#70a7ff";
            if(d.tool==="measure"){
              const distance=d.b.price-d.a.price;
              const pct=((d.b.price-d.a.price)/Math.max(Math.abs(d.a.price),Number.EPSILON))*100;
              const ia=candles.reduce((best,candle,i)=>Math.abs((candle.time as number)-(d.a.time as number))<Math.abs((candles[best]?.time as number ?? Infinity)-(d.a.time as number))?i:best,0);
              const ib=candles.reduce((best,candle,i)=>Math.abs((candle.time as number)-(d.b.time as number))<Math.abs((candles[best]?.time as number ?? Infinity)-(d.b.time as number))?i:best,0);
              const bars=Math.abs(ib-ia);
              return <g key={d.id}>{hit(x1,y1,x2,y2)}<line x1={x1} y1={y1} x2={x2} y2={y2} stroke={selectedDrawingId===d.id?"#fbbf24":"#94a3b8"} strokeWidth={2}/><text x={(x1+x2)/2} y={(y1+y2)/2-8} fill="#d8e0ea" fontSize="11" textAnchor="middle">{distance.toFixed(meta?.digits??5)} · {pct.toFixed(2)}% · {bars} bars</text></g>;
            }
            if(d.tool==="long"||d.tool==="short"){
              const entry=d.a.price, target=d.b.price, risk=Math.abs(target-entry), stop=d.tool==="long"?entry-risk:entry+risk, reward=Math.abs(target-entry), rr=reward/Math.max(Math.abs(entry-stop),Number.EPSILON);
              const top=Math.min(y1,y2), bottom=Math.max(y1,y2);
              return <g key={d.id}>{hit(x1,y1,x2,y2)}<rect x={Math.min(x1,x2)} y={top} width={Math.max(40,Math.abs(x2-x1))} height={Math.max(1,bottom-top)} fill={d.tool==="long"?"rgba(34,197,94,.10)":"rgba(239,68,80,.10)"} stroke={stroke} strokeWidth={selectedDrawingId===d.id?2:1}/><line x1={Math.min(x1,x2)} x2={Math.max(x1,x2)+40} y1={y1} y2={y1} stroke="#fbbf24" strokeWidth="2"/><line x1={Math.min(x1,x2)} x2={Math.max(x1,x2)+40} y1={d.tool==="long"?mainRef.current?.priceToCoordinate(stop)??y1:mainRef.current?.priceToCoordinate(stop)??y1} y2={d.tool==="long"?mainRef.current?.priceToCoordinate(stop)??y1:mainRef.current?.priceToCoordinate(stop)??y1} stroke="#ef5350" strokeWidth="1" strokeDasharray="4 3"/><text x={Math.max(x1,x2)+45} y={y1-6} fill="#d8e0ea" fontSize="10">{d.tool==="long"?"LONG":"SHORT"} · R:R {rr.toFixed(2)}</text></g>;
            }
            return <g key={d.id}>{hit(x1,y1,x2,y2)}<line x1={x1} y1={y1} x2={x2} y2={y2} stroke={selectedDrawingId===d.id?"#fbbf24":stroke} strokeWidth={selectedDrawingId===d.id?3:(d.tool==="trendline"||d.tool==="ray"||d.tool==="long"||d.tool==="short"?2:1)}/></g>;
          })}
          {pendingPoint && <circle cx={chartRef.current?.timeScale().timeToCoordinate(pendingPoint.time) ?? 0} cy={mainRef.current?.priceToCoordinate(pendingPoint.price) ?? 0} r="4" fill="#fbbf24"/>}
        </svg>
        <div ref={host} className="absolute inset-0"/>
        {!candles.length&&<div className="pointer-events-none absolute inset-0 flex items-center justify-center"><div className="rounded-lg border border-[#293748] bg-[#0d131b]/95 px-8 py-6 text-center shadow-xl"><div className="text-lg font-semibold">{t(locale,"noData")}</div><div className="mt-2 max-w-lg text-xs leading-5 text-[#718096]">CFIP renders normalized market observations only. No synthetic candles are generated.</div></div></div>}
      </section>
      {sidebar&&<TerminalSidebar locale={locale} tab={tab} setTab={setTab} symbol={symbol} candles={candles} analysis={canonicalAnalysis} collapsed={false} setCollapsed={toggleSidebar} preferences={prefs} setPreferences={setPrefs} drawings={drawings} setDrawings={updateDrawings} structurePoints={structure.points} structureEvents={structure.events} orderBlocks={blocks} selectedDrawingId={selectedDrawingId} setSelectedDrawingId={setSelectedDrawingId} setSymbol={setSymbol}/>}
    </div>
    <footer className="cfip-terminal-footer flex h-7 shrink-0 items-center justify-between border-t border-[#27313d] bg-[#0d131b] px-3 text-[10px] text-[#687689]">
      <span>{t(locale,"marketData")} · {live?"LIVE":"WAITING"} · {candles.length} bars</span>
      <ChartAttribution locale="en"/>
    </footer>
  </div>;
}
