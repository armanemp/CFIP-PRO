"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { ColorType, CrosshairMode, createChart, type IChartApi, type ISeriesApi, type SeriesType } from "lightweight-charts";
import type { MarketObservation } from "@/lib/api";
import { getMarketObservations } from "@/lib/api";
import { TerminalSidebar } from "@/components/terminal/terminal-sidebar";
import { SymbolPicker } from "@/components/terminal/symbol-picker";
import { ChartAttribution } from "@/components/terminal/chart-attribution";
import { forexSymbols } from "@/components/terminal/symbols";
import { t, localeNames, rtlLocales } from "@/components/terminal/i18n";
import { DEFAULT_PREFERENCES, type ChartKind, type ChartPreferences, type Drawing, type InspectorTab, type Locale, type Timeframe, type Tool } from "@/components/terminal/types";
import { aggregateAnalysis, type UnifiedAnalysis } from "@/components/terminal/analysis-contracts";
import "./terminal/terminal-theme.module.css";
import { ema, bollinger, sma, wma, vwap, toCandles, rsi, macd, fvg, pivots, supportResistance, sessionRange, marketStructure, orderBlocks, liquidityAnalysis, displacementAnalysis, premiumDiscount, mtfStructure, atr, dmi, stochastic, donchian, keltner, ichimoku } from "@/components/terminal/chart-math";
import { addIndicatorSeries, addMainSeries, addVolumeSeries, setMainSeriesData } from "@/components/terminal/chart-engine";
import { clearTerminalSession, loadTerminalSession, saveTerminalSession } from "@/components/terminal/session-storage";

const tfs: Timeframe[] = ["1m","5m","15m","30m","1H","4H","1D","1W","1M"];
const studies = ["EMA20","EMA50","EMA200","SMA20","WMA20","VWAP","BB20","RSI14","MACD","DMI14","STOCH14","DONCHIAN20","KELTNER20","ICHIMOKU"] as const;
const tools: Tool[] = ["cursor","crosshair","trendline","ray","horizontal","vertical","rectangle","fib","measure","long","short"];
const toolGlyph: Record<Tool,string> = {cursor:"•",crosshair:"✛",trendline:"╱",ray:"↗",horizontal:"—",vertical:"│",rectangle:"□",fib:"F",measure:"↔",long:"↗",short:"↘"};

export function ProfessionalChartTerminalV3({ observations: initial, symbol: initialSymbol }: { observations: MarketObservation[]; symbol: string }) {
  const host=useRef<HTMLDivElement>(null);
  const chartRef=useRef<IChartApi|null>(null);
  const mainRef=useRef<ISeriesApi<SeriesType>|null>(null);
  const [symbol,setSymbol]=useState(initialSymbol),[rows,setRows]=useState(initial),[tf,setTf]=useState<Timeframe>("1m"),[kind,setKind]=useState<ChartKind>("candles"),[locale,setLocale]=useState<Locale>("en"),[sidebar,setSidebar]=useState(DEFAULT_PREFERENCES.rightSidebar),[rail,setRail]=useState(DEFAULT_PREFERENCES.leftRail),[tab,setTab]=useState<InspectorTab>("market"),[panel,setPanel]=useState<string|null>(null),[tool,setTool]=useState<Tool>("cursor"),[selected,setSelected]=useState<string[]>(["EMA20"]),[prefs,setPrefs]=useState<ChartPreferences>(DEFAULT_PREFERENCES),[drawings,setDrawings]=useState<Drawing[]>([]),[pendingPoint,setPendingPoint]=useState<Drawing["a"]|null>(null),[live,setLive]=useState(false),[error,setError]=useState(false);

  const candles=useMemo(()=>toCandles(rows,tf),[rows,tf]);
  const last=candles.at(-1),prev=candles.at(-2);
  const zones=useMemo(()=>fvg(candles),[candles]);
  const pivotPoints=useMemo(()=>pivots(candles),[candles]);
  const levels=useMemo(()=>supportResistance(candles),[candles]);
  const session=useMemo(()=>sessionRange(candles),[candles]);
  const structure=useMemo(()=>marketStructure(candles),[candles]);
  const blocks=useMemo(()=>orderBlocks(candles),[candles]);
  const liquidity=useMemo(()=>liquidityAnalysis(candles),[candles]);
  const displacement=useMemo(()=>displacementAnalysis(candles),[candles]);
  const pd=useMemo(()=>premiumDiscount(candles),[candles]);
  const mtf=useMemo(()=>mtfStructure(candles,tf),[candles,tf]);
  const rsiValue=useMemo(()=>rsi(candles,14).at(-1)?.value ?? null,[candles]);
  const macdValue=useMemo(()=>macd(candles).histogram.at(-1)?.value ?? null,[candles]);
  const atrValue=useMemo(()=>atr(candles,14).at(-1)?.value ?? null,[candles]);
  const analysisCandles = candles.length > 1 ? candles.slice(0, -1) : candles;
  const analysisZones = useMemo(() => fvg(analysisCandles), [analysisCandles]);
  const analysisStructure = useMemo(() => marketStructure(analysisCandles), [analysisCandles]);
  const analysisBlocks = useMemo(() => orderBlocks(analysisCandles), [analysisCandles]);
  const analysisLiquidity = useMemo(() => liquidityAnalysis(analysisCandles), [analysisCandles]);
  const analysisDisplacement = useMemo(() => displacementAnalysis(analysisCandles), [analysisCandles]);
  const analysisPd = useMemo(() => premiumDiscount(analysisCandles), [analysisCandles]);
  const analysisMtf = useMemo(() => mtfStructure(analysisCandles, tf), [analysisCandles, tf]);
  const analysisRsi = useMemo(() => rsi(analysisCandles,14).at(-1)?.value ?? null, [analysisCandles]);
  const analysisMacd = useMemo(() => macd(analysisCandles).histogram.at(-1)?.value ?? null, [analysisCandles]);
  const analysisAtr = useMemo(() => atr(analysisCandles,14).at(-1)?.value ?? null, [analysisCandles]);
  const analysis=useMemo<UnifiedAnalysis>(()=>aggregateAnalysis({
    candles:analysisCandles,zones:analysisZones,structurePoints:analysisStructure.points,structureEvents:analysisStructure.events,orderBlocks:analysisBlocks,
    liquidityPools:analysisLiquidity.pools,liquiditySweeps:analysisLiquidity.sweeps,displacement:analysisDisplacement,premiumDiscount:analysisPd,mtf:analysisMtf,
    rsi:analysisRsi,macdHistogram:analysisMacd,atr:analysisAtr,
  }),[analysisCandles,analysisZones,analysisStructure,analysisBlocks,analysisLiquidity,analysisDisplacement,analysisPd,analysisMtf,analysisRsi,analysisMacd,analysisAtr]);
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
  }, [initialSymbol]);

  useEffect(() => {
    saveTerminalSession({
      symbol, timeframe: tf, chartKind: kind, locale, tool,
      selectedStudies: selected, preferences: { ...prefs, rightSidebar: sidebar, leftRail: rail }, drawings,
    });
  }, [symbol, tf, kind, locale, tool, selected, prefs, sidebar, rail, drawings]);

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) return;
      if (event.key === "Escape") { setPanel(null); setTool("cursor"); return; }
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
    let active=true;
    const load=async()=>{
      try{
        const next=await getMarketObservations(symbol,"reference",5000);
        if(active){setRows(next);setLive(next.length>0);setError(false);}
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
  },[locale,prefs.showGrid]);

  useEffect(()=>{
    const c=chartRef.current;
    if(!c||!candles.length)return;
    for(const pane of c.panes())for(const s of pane.getSeries())c.removeSeries(s);

    const main=addMainSeries(c,kind,candles[0].close);
    setMainSeriesData(main,kind,candles);
    mainRef.current=main;

    if(selected.includes("EMA20"))addIndicatorSeries(c,ema(candles,20),"#60a5fa","EMA 20");
    if(selected.includes("EMA50"))addIndicatorSeries(c,ema(candles,50),"#c084fc","EMA 50");
    if(selected.includes("EMA200"))addIndicatorSeries(c,ema(candles,200),"#f97316","EMA 200");
    if(selected.includes("SMA20"))addIndicatorSeries(c,sma(candles,20),"#fbbf24","SMA 20");
    if(selected.includes("WMA20"))addIndicatorSeries(c,wma(candles,20),"#fb923c","WMA 20");
    if(selected.includes("VWAP"))addIndicatorSeries(c,vwap(candles),"#34d399","VWAP");
    if(selected.includes("RSI14")) addIndicatorSeries(c, rsi(candles,14), "#e879f9", "RSI 14");
    if(selected.includes("MACD")) {
      const m=macd(candles);
      addIndicatorSeries(c,m.macd,"#38bdf8","MACD");
      addIndicatorSeries(c,m.signal,"#f59e0b","MACD signal");
    }
    if(selected.includes("DMI14")) {
      const d=dmi(candles,14);
      addIndicatorSeries(c,d.map(x=>({time:x.time,value:x.plus})), "#22c55e", "DMI +DI");
      addIndicatorSeries(c,d.map(x=>({time:x.time,value:x.minus})), "#ef4444", "DMI -DI");
      addIndicatorSeries(c,d.map(x=>({time:x.time,value:x.adx})), "#a78bfa", "ADX");
    }
    if(selected.includes("STOCH14")) addIndicatorSeries(c,stochastic(candles,14,3),"#f472b6","Stochastic 14");
    if(selected.includes("DONCHIAN20")) {
      const d=donchian(candles,20);
      addIndicatorSeries(c,d.map(x=>({time:x.time,value:x.upper})),"#64748b","Donchian upper");
      addIndicatorSeries(c,d.map(x=>({time:x.time,value:x.middle})),"#94a3b8","Donchian mid");
      addIndicatorSeries(c,d.map(x=>({time:x.time,value:x.lower})),"#64748b","Donchian lower");
    }
    if(selected.includes("KELTNER20")) {
      const k=keltner(candles,20,14,1.5);
      addIndicatorSeries(c,k.map(x=>({time:x.time,value:x.upper})),"#0ea5e9","Keltner upper");
      addIndicatorSeries(c,k.map(x=>({time:x.time,value:x.middle})),"#38bdf8","Keltner mid");
      addIndicatorSeries(c,k.map(x=>({time:x.time,value:x.lower})),"#0ea5e9","Keltner lower");
    }
    if(selected.includes("ICHIMOKU")) {
      const i=ichimoku(candles);
      addIndicatorSeries(c,i.map(x=>({time:x.time,value:x.tenkan})),"#f43f5e","Ichimoku Tenkan");
      addIndicatorSeries(c,i.map(x=>({time:x.time,value:x.kijun})),"#f59e0b","Ichimoku Kijun");
      addIndicatorSeries(c,i.map(x=>({time:x.time,value:x.senkouA})),"#22c55e","Ichimoku Span A");
      addIndicatorSeries(c,i.map(x=>({time:x.time,value:x.senkouB})),"#a855f7","Ichimoku Span B");
    }
    if(selected.includes("BB20")){
      const b=bollinger(candles);
      addIndicatorSeries(c,b.map(x=>({time:x.time,value:x.upper})),"#64748b","BB upper");
      addIndicatorSeries(c,b.map(x=>({time:x.time,value:x.mid})),"#94a3b8","BB mid");
      addIndicatorSeries(c,b.map(x=>({time:x.time,value:x.lower})),"#64748b","BB lower");
    }
    if(prefs.showVolume)addVolumeSeries(c,candles);
    c.timeScale().fitContent();
  },[candles,kind,selected,prefs.showVolume]);

  const toggleSidebar=(value:boolean)=>{setSidebar(value);setPrefs(p=>({...p,rightSidebar:value}));};
  const toggleRail=(value:boolean)=>{setRail(value);setPrefs(p=>({...p,leftRail:value}));};

  const tt=(x:Tool)=>({
    cursor:t(locale,"cursor"),crosshair:t(locale,"crosshair"),trendline:t(locale,"trendline"),ray:t(locale,"ray"),
    horizontal:t(locale,"horizontal"),vertical:t(locale,"vertical"),rectangle:t(locale,"rectangle"),fib:t(locale,"fibonacci"),
    measure:t(locale,"measure"),long:t(locale,"longPosition"),short:t(locale,"shortPosition")
  }[x]);

  const toggle=(id:string)=>setSelected(s=>s.includes(id)?s.filter(x=>x!==id):[...s,id]);

  const resetView=()=>chartRef.current?.timeScale().fitContent();
  const placeDrawing=(event: React.MouseEvent<HTMLElement>)=>{
    if(tool==="cursor"||tool==="crosshair"||!chartRef.current||!mainRef.current)return;
    const rect=event.currentTarget.getBoundingClientRect();
    const x=event.clientX-rect.left, y=event.clientY-rect.top;
    const time=chartRef.current.timeScale().coordinateToTime(x);
    const price=mainRef.current.coordinateToPrice(y);
    if(time===null||price===null)return;
    const point={time,price};
    if(!pendingPoint){setPendingPoint(point);return;}
    const drawing:Drawing={id:crypto.randomUUID(),tool:tool as Drawing["tool"],a:pendingPoint,b:point,visible:true,locked:false};
    setDrawings(value=>[...value,drawing]);
    setPendingPoint(null);
  };
  const toggleFullscreen=async()=>{
    const element=host.current?.parentElement;
    if(!element)return;
    if(document.fullscreenElement)await document.exitFullscreen();
    else await element.requestFullscreen();
  };

  return <div dir={rtlLocales.has(locale)?"rtl":"ltr"} className="cfip-terminal relative flex h-full min-h-0 flex-col bg-[#080b10] text-[#d8e0ea]">
    <header className="cfip-terminal-topbar relative flex h-12 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-2">
      <button onClick={()=>toggleRail(!rail)} title={rail?t(locale,"hideRail"):t(locale,"showRail")} className="mr-2 rounded border border-[#334155] px-2 py-1.5 text-xs">☰</button>
      <button onClick={()=>setPanel(panel==="symbol"?null:"symbol")} className="flex min-w-[180px] items-center gap-2 rounded px-2 py-1.5 text-left hover:bg-[#17202c]"><strong className="text-[15px]">{symbol}</strong><span className="text-[10px] text-[#66758a]">{meta?.name??"Forex"}</span></button>
      {panel==="symbol"&&<SymbolPicker locale={locale} value={symbol} onChange={s=>{setSymbol(s.symbol);setPanel(null)}}/>}
      <span className="mx-2 h-5 w-px bg-[#293342]"/>
      <div className="flex gap-1">{tfs.map(x=><button key={x} onClick={()=>setTf(x)} className={`rounded px-2.5 py-1.5 text-xs ${tf===x?"bg-[#23364d] text-white":"text-[#8391a4] hover:bg-[#17202c]"}`}>{x}</button>)}<button onClick={()=>setPanel(panel==="timeframe"?null:"timeframe")} className="rounded px-2 text-[#8391a4]">⋯</button></div>
      <div className="ml-auto flex items-center gap-1">
        <button onClick={resetView} title="R" className="rounded px-2.5 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"autoFit")}</button>
        <button onClick={()=>{const c=chartRef.current;if(!c)return;const canvas=c.takeScreenshot();const link=document.createElement("a");link.download=`cfip-${symbol.replace("/","-")}-${tf}.png`;link.href=canvas.toDataURL("image/png");link.click();}} className="rounded px-2.5 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"screenshot")}</button>
        <button onClick={toggleFullscreen} className="rounded px-2.5 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"fullscreen")}</button>
        <button onClick={()=>setPanel(panel==="indicators"?null:"indicators")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"indicators")}</button>
        <button onClick={()=>setPanel(panel==="chartType"?null:"chartType")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"chartType")}</button>
        <button onClick={()=>setPanel(panel==="language"?null:"language")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{locale.toUpperCase()}</button>
        <button onClick={()=>setPanel(panel==="settings"?null:"settings")} className="rounded px-3 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"settings")}</button>
      </div>
      {panel==="timeframe"&&<div className="absolute right-52 top-11 z-50 grid w-60 grid-cols-3 gap-1 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{tfs.map(x=><button key={x} onClick={()=>{setTf(x);setPanel(null)}} className="rounded px-2 py-2 text-xs hover:bg-[#17202c]">{x}</button>)}</div>}
      {panel==="chartType"&&<div className="absolute right-40 top-11 z-50 w-44 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{(["candles","bars","line","area","baseline"] as ChartKind[]).map(x=><button key={x} onClick={()=>{setKind(x);setPanel(null)}} className="block w-full rounded px-3 py-2 text-left text-xs hover:bg-[#17202c]">{t(locale,x==="candles"?"candlestick":x)}</button>)}</div>}
      {panel==="indicators"&&<div className="absolute right-28 top-11 z-50 grid w-64 grid-cols-2 gap-1 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{studies.map(x=><button key={x} onClick={()=>toggle(x)} className={`rounded px-3 py-2 text-left text-xs ${selected.includes(x)?"bg-[#20354b] text-white":"hover:bg-[#17202c]"}`}>{x}</button>)}</div>}
      {panel==="language"&&<div className="absolute right-2 top-11 z-50 grid w-64 grid-cols-2 gap-1 rounded-lg border border-[#334155] bg-[#0d131b] p-2 shadow-2xl">{(Object.keys(localeNames) as Locale[]).map(x=><button key={x} onClick={()=>{setLocale(x);setPanel(null)}} className="rounded px-3 py-2 text-left text-xs hover:bg-[#17202c]">{localeNames[x]}</button>)}</div>}
      {panel==="settings"&&<div className="absolute right-2 top-11 z-50 w-72 rounded-lg border border-[#334155] bg-[#0d131b] p-3 shadow-2xl">
        {([
          ["showGrid","grid"],["showVolume","volume"],["showSessions","sessions"],["showBidAsk","bidAsk"],["magnet","magnet"]
        ] as const).map(([key,label])=><label key={key} className="flex items-center justify-between border-b border-[#1f2937] px-2 py-2.5 text-xs last:border-0"><span>{t(locale,label)}</span><input type="checkbox" checked={prefs[key]} onChange={e=>setPrefs({...prefs,[key]:e.target.checked})}/></label>)}
      </div>}
    </header>
    <div className="flex min-h-0 flex-1">
      {rail&&<nav className="cfip-terminal-rail flex w-12 shrink-0 flex-col items-center gap-1 border-r border-[#27313d] bg-[#0b1017] py-2">{tools.map(x=><button key={x} onClick={()=>setTool(x)} title={tt(x)} aria-label={tt(x)} className={`h-9 w-9 rounded text-xs ${tool===x?"bg-[#20354b] text-white":"text-[#8290a3] hover:bg-[#17202c]"}`}>{toolGlyph[x]}</button>)}</nav>}
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
        <svg aria-label="Chart drawings" className="pointer-events-none absolute inset-0 z-10 h-full w-full overflow-visible">
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
            if(d.tool==="horizontal") return <line key={d.id} x1={0} x2="100%" y1={y1} y2={y1} stroke="#94a3b8" strokeWidth="1" strokeDasharray="5 4"/>;
            if(d.tool==="vertical") return <line key={d.id} x1={x1} x2={x1} y1={0} y2="100%" stroke="#94a3b8" strokeWidth="1" strokeDasharray="5 4"/>;
            if(d.tool==="rectangle") return <rect key={d.id} x={Math.min(x1,x2)} y={Math.min(y1,y2)} width={Math.abs(x2-x1)} height={Math.abs(y2-y1)} fill="rgba(112,167,255,.08)" stroke="#70a7ff" strokeWidth="1"/>;
            if(d.tool==="fib") return <g key={d.id}><line x1={x1} y1={y1} x2={x2} y2={y2} stroke="#fbbf24" strokeWidth="1"/><line x1={0} x2="100%" y1={y1+(y2-y1)*.382} y2={y1+(y2-y1)*.382} stroke="#fbbf24" strokeWidth="1" strokeDasharray="3 3"/><line x1={0} x2="100%" y1={y1+(y2-y1)*.618} y2={y1+(y2-y1)*.618} stroke="#fbbf24" strokeWidth="1" strokeDasharray="3 3"/></g>;
            const stroke=d.tool==="short"?"#ef5350":"#70a7ff";
            return <line key={d.id} x1={x1} y1={y1} x2={x2} y2={y2} stroke={stroke} strokeWidth={d.tool==="trendline"||d.tool==="ray"||d.tool==="long"||d.tool==="short"?2:1}/>;
          })}
          {pendingPoint && <circle cx={chartRef.current?.timeScale().timeToCoordinate(pendingPoint.time) ?? 0} cy={mainRef.current?.priceToCoordinate(pendingPoint.price) ?? 0} r="4" fill="#fbbf24"/>}
        </svg>
        <div ref={host} className="absolute inset-0"/>
        {!candles.length&&<div className="pointer-events-none absolute inset-0 flex items-center justify-center"><div className="rounded-lg border border-[#293748] bg-[#0d131b]/95 px-8 py-6 text-center shadow-xl"><div className="text-lg font-semibold">{t(locale,"noData")}</div><div className="mt-2 max-w-lg text-xs leading-5 text-[#718096]">CFIP renders normalized market observations only. No synthetic candles are generated.</div></div></div>}
      </section>
      {sidebar&&<TerminalSidebar locale={locale} tab={tab} setTab={setTab} symbol={symbol} candles={candles} analysis={analysis} collapsed={false} setCollapsed={toggleSidebar} preferences={prefs} setPreferences={setPrefs} drawings={drawings} setDrawings={setDrawings} structurePoints={structure.points} structureEvents={structure.events} orderBlocks={blocks}/>}
    </div>
    <footer className="cfip-terminal-footer flex h-7 shrink-0 items-center justify-between border-t border-[#27313d] bg-[#0d131b] px-3 text-[10px] text-[#687689]">
      <span>{t(locale,"marketData")} · {live?"LIVE":"WAITING"} · {candles.length} bars</span>
      <ChartAttribution locale={locale}/>
    </footer>
  </div>;
}
