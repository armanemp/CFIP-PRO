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
import { DEFAULT_PREFERENCES, type ChartKind, type ChartPreferences, type InspectorTab, type Locale, type Timeframe, type Tool } from "@/components/terminal/types";
import { ema, bollinger, sma, wma, vwap, toCandles } from "@/components/terminal/chart-math";
import { addIndicatorSeries, addMainSeries, addVolumeSeries, setMainSeriesData } from "@/components/terminal/chart-engine";

const tfs: Timeframe[] = ["1m","5m","15m","30m","1H","4H","1D","1W","1M"];
const studies = ["EMA20","EMA50","SMA20","WMA20","VWAP","BB20"] as const;
const tools: Tool[] = ["cursor","crosshair","trendline","ray","horizontal","vertical","rectangle","fib","measure","long","short"];
const toolGlyph: Record<Tool,string> = {cursor:"•",crosshair:"✛",trendline:"╱",ray:"↗",horizontal:"—",vertical:"│",rectangle:"□",fib:"F",measure:"↔",long:"↗",short:"↘"};

export function ProfessionalChartTerminalV3({ observations: initial, symbol: initialSymbol }: { observations: MarketObservation[]; symbol: string }) {
  const host=useRef<HTMLDivElement>(null);
  const chartRef=useRef<IChartApi|null>(null);
  const mainRef=useRef<ISeriesApi<SeriesType>|null>(null);
  const [symbol,setSymbol]=useState(initialSymbol),[rows,setRows]=useState(initial),[tf,setTf]=useState<Timeframe>("1m"),[kind,setKind]=useState<ChartKind>("candles"),[locale,setLocale]=useState<Locale>("en"),[sidebar,setSidebar]=useState(true),[rail,setRail]=useState(true),[tab,setTab]=useState<InspectorTab>("market"),[panel,setPanel]=useState<string|null>(null),[tool,setTool]=useState<Tool>("cursor"),[selected,setSelected]=useState<string[]>(["EMA20"]),[prefs,setPrefs]=useState<ChartPreferences>(DEFAULT_PREFERENCES),[live,setLive]=useState(false),[error,setError]=useState(false);

  const candles=useMemo(()=>toCandles(rows,tf),[rows,tf]);
  const last=candles.at(-1),prev=candles.at(-2);
  const pct=last&&prev?((last.close-prev.close)/prev.close)*100:0;
  const meta=forexSymbols.find(x=>x.symbol===symbol);

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
    document.documentElement.dir=rtlLocales.has(locale)?"rtl":"ltr";
    document.documentElement.lang=locale;
  },[locale]);

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
    if(selected.includes("SMA20"))addIndicatorSeries(c,sma(candles,20),"#fbbf24","SMA 20");
    if(selected.includes("WMA20"))addIndicatorSeries(c,wma(candles,20),"#fb923c","WMA 20");
    if(selected.includes("VWAP"))addIndicatorSeries(c,vwap(candles),"#34d399","VWAP");
    if(selected.includes("BB20")){
      const b=bollinger(candles);
      addIndicatorSeries(c,b.map(x=>({time:x.time,value:x.upper})),"#64748b","BB upper");
      addIndicatorSeries(c,b.map(x=>({time:x.time,value:x.mid})),"#94a3b8","BB mid");
      addIndicatorSeries(c,b.map(x=>({time:x.time,value:x.lower})),"#64748b","BB lower");
    }
    if(prefs.showVolume)addVolumeSeries(c,candles);
    c.timeScale().fitContent();
  },[candles,kind,selected,prefs.showVolume]);

  const tt=(x:Tool)=>({
    cursor:t(locale,"cursor"),crosshair:t(locale,"crosshair"),trendline:t(locale,"trendline"),ray:t(locale,"ray"),
    horizontal:t(locale,"horizontal"),vertical:t(locale,"vertical"),rectangle:t(locale,"rectangle"),fib:t(locale,"fibonacci"),
    measure:t(locale,"measure"),long:t(locale,"longPosition"),short:t(locale,"shortPosition")
  }[x]);

  const toggle=(id:string)=>setSelected(s=>s.includes(id)?s.filter(x=>x!==id):[...s,id]);

  const resetView=()=>chartRef.current?.timeScale().fitContent();
  const toggleFullscreen=async()=>{
    const element=host.current?.parentElement;
    if(!element)return;
    if(document.fullscreenElement)await document.exitFullscreen();
    else await element.requestFullscreen();
  };

  return <div dir={rtlLocales.has(locale)?"rtl":"ltr"} className="relative flex h-full min-h-0 flex-col bg-[#080b10] text-[#d8e0ea]">
    <header className="relative flex h-12 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-2">
      <button onClick={()=>setRail(x=>!x)} title={rail?t(locale,"hideRail"):t(locale,"showRail")} className="mr-2 rounded border border-[#334155] px-2 py-1.5 text-xs">☰</button>
      <button onClick={()=>setPanel(panel==="symbol"?null:"symbol")} className="flex min-w-[180px] items-center gap-2 rounded px-2 py-1.5 text-left hover:bg-[#17202c]"><strong className="text-[15px]">{symbol}</strong><span className="text-[10px] text-[#66758a]">{meta?.name??"Forex"}</span></button>
      {panel==="symbol"&&<SymbolPicker locale={locale} value={symbol} onChange={s=>{setSymbol(s.symbol);setPanel(null)}}/>}
      <span className="mx-2 h-5 w-px bg-[#293342]"/>
      <div className="flex gap-1">{tfs.slice(0,6).map(x=><button key={x} onClick={()=>setTf(x)} className={`rounded px-2.5 py-1.5 text-xs ${tf===x?"bg-[#23364d] text-white":"text-[#8391a4] hover:bg-[#17202c]"}`}>{x}</button>)}<button onClick={()=>setPanel(panel==="timeframe"?null:"timeframe")} className="rounded px-2 text-[#8391a4]">⋯</button></div>
      <div className="ml-auto flex items-center gap-1">
        <button onClick={resetView} className="rounded px-2.5 py-1.5 text-xs hover:bg-[#17202c]">{t(locale,"autoFit")}</button>
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
      {rail&&<nav className="flex w-12 shrink-0 flex-col items-center gap-1 border-r border-[#27313d] bg-[#0b1017] py-2">{tools.map(x=><button key={x} onClick={()=>setTool(x)} title={tt(x)} aria-label={tt(x)} className={`h-9 w-9 rounded text-xs ${tool===x?"bg-[#20354b] text-white":"text-[#8290a3] hover:bg-[#17202c]"}`}>{toolGlyph[x]}</button>)}</nav>}
      <section className="relative min-w-0 flex-1">
        <div className="absolute left-3 top-2 z-20 flex items-center gap-3 text-xs">
          <strong className="text-white">{symbol}</strong><span className="text-[#8492a5]">{tf}</span>
          {last&&<><span>O {last.open.toFixed(meta?.digits??5)}</span><span>H {last.high.toFixed(meta?.digits??5)}</span><span>L {last.low.toFixed(meta?.digits??5)}</span><span>C {last.close.toFixed(meta?.digits??5)}</span><span className={pct>=0?"text-emerald-400":"text-red-400"}>{pct>=0?"+":""}{pct.toFixed(2)}%</span></>}
          <span className={live?"text-emerald-400":"text-amber-400"}>● {live?t(locale,"live"):error?t(locale,"dataOffline"):t(locale,"loading")}</span>
        </div>
        <div ref={host} className="absolute inset-0"/>
        {!candles.length&&<div className="pointer-events-none absolute inset-0 flex items-center justify-center"><div className="rounded-lg border border-[#293748] bg-[#0d131b]/95 px-8 py-6 text-center shadow-xl"><div className="text-lg font-semibold">{t(locale,"noData")}</div><div className="mt-2 max-w-lg text-xs leading-5 text-[#718096]">CFIP renders normalized market observations only. No synthetic candles are generated.</div></div></div>}
      </section>
      {sidebar&&<TerminalSidebar locale={locale} tab={tab} setTab={setTab} symbol={symbol} candles={candles} collapsed={false} setCollapsed={setSidebar} preferences={prefs} setPreferences={setPrefs}/>}
    </div>
    <footer className="flex h-7 shrink-0 items-center justify-between border-t border-[#27313d] bg-[#0d131b] px-3 text-[10px] text-[#687689]">
      <span>{t(locale,"marketData")} · {live?"LIVE":"WAITING"} · {candles.length} bars</span>
      <ChartAttribution locale={locale}/>
    </footer>
  </div>;
}
