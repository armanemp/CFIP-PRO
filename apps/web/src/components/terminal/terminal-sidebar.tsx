"use client";

import type { Candle, Drawing, InspectorTab, Locale, ChartPreferences, OrderBlock, StructureEvent, StructurePoint } from "./types";
import type { UnifiedAnalysis } from "./analysis-contracts";
import { t } from "./i18n";
import "./terminal-theme.module.css";

const tabsA: InspectorTab[] = ["market", "watchlist", "structure"];
const tabsB: InspectorTab[] = ["intelligence", "risk", "objects"];

function biasLabel(analysis: UnifiedAnalysis) {
  return analysis.bias === "bullish" ? "Bullish" : analysis.bias === "bearish" ? "Bearish" : "Neutral";
}

export function TerminalSidebar({
  locale, tab, setTab, symbol, candles, collapsed, setCollapsed, preferences, setPreferences,
  drawings, setDrawings, selectedDrawingId, setSelectedDrawingId, structurePoints, structureEvents, orderBlocks, analysis,
}: {
  locale: Locale; tab: InspectorTab; setTab: (v: InspectorTab) => void; symbol: string; candles: Candle[];
  collapsed: boolean; setCollapsed: (v: boolean) => void; preferences: ChartPreferences;
  setPreferences: (v: ChartPreferences) => void; drawings: Drawing[]; setDrawings: (v: Drawing[]) => void;
  structurePoints: StructurePoint[]; structureEvents: StructureEvent[]; orderBlocks: OrderBlock[];
  analysis: UnifiedAnalysis;
}) {
  const last = candles.at(-1);
  const prev = candles.at(-2);
  const change = last && prev ? ((last.close - prev.close) / prev.close) * 100 : 0;
  if (collapsed) return <button onClick={() => setCollapsed(false)} aria-label={t(locale, "showSidebar")} title={t(locale, "showSidebar")} className="absolute right-2 top-2 z-30 rounded border border-[#334155] bg-[#0d131b] px-2 py-2 text-xs text-[#c8d2df] shadow-lg">‹</button>;

  return <aside className="cfip-terminal-sidebar relative flex w-[300px] shrink-0 flex-col border-l border-[#27313d] bg-[#0d131b]">
    <button onClick={() => setCollapsed(true)} aria-label={t(locale, "hideSidebar")} title={t(locale, "hideSidebar")} className="absolute right-2 top-2 z-10 rounded border border-[#334155] px-2 py-1 text-xs text-[#94a3b8] hover:text-white">›</button>
    <div className="grid grid-cols-3 border-b border-[#27313d] p-1 pr-10">{tabsA.map(x => <button key={x} onClick={() => setTab(x)} className={`rounded px-2 py-2 text-[11px] ${tab === x ? "bg-[#1b2b3f] text-white" : "text-[#718096]"}`}>{t(locale, x)}</button>)}</div>
    <div className="grid grid-cols-3 border-b border-[#27313d] p-1">{tabsB.map(x => <button key={x} onClick={() => setTab(x)} className={`rounded px-2 py-2 text-[11px] ${tab === x ? "bg-[#1b2b3f] text-white" : "text-[#718096]"}`}>{t(locale, x)}</button>)}</div>

    <div className="min-h-0 flex-1 overflow-auto p-3 text-sm">
      {tab === "market" && <div className="space-y-3">
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">
          <div className="text-xs text-[#718096]">{symbol}</div>
          <div className="mt-1 text-2xl font-semibold tabular-nums">{last ? last.close.toFixed(5) : "—"}</div>
          <div className={change >= 0 ? "text-emerald-400" : "text-red-400"}>{last ? `${change >= 0 ? "+" : ""}${change.toFixed(2)}%` : "—"}</div>
        </div>
        <div className="grid grid-cols-2 gap-2">{[[t(locale,"open"),last?.open],[t(locale,"high"),last?.high],[t(locale,"low"),last?.low],[t(locale,"close"),last?.close],[t(locale,"volume"),last?.volume]].map(([k,v])=>
          <div key={String(k)} className="rounded border border-[#263241] bg-[#0a0f16] p-2"><div className="text-[10px] uppercase text-[#64748b]">{k}</div><div className="mt-1 tabular-nums">{typeof v === "number" ? v.toFixed(5) : "—"}</div></div>)}</div>
      </div>}

      {tab === "watchlist" && <div className="space-y-1.5">{["EUR/USD","GBP/USD","USD/JPY","USD/CHF","AUD/USD","USD/CAD","NZD/USD","EUR/JPY"].map(s =>
        <div key={s} className="flex min-h-9 items-center justify-between rounded border border-[#263241] px-3 py-2"><span>{s}</span><span className="text-xs text-[#64748b]">{s === symbol && last ? last.close.toFixed(5) : "—"}</span></div>)}</div>}

      {tab === "structure" && <div className="space-y-3">
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">
          <div className="flex items-center justify-between"><span className="text-xs uppercase text-[#64748b]">Unified bias</span><span className="font-semibold">{biasLabel(analysis)}</span></div>
          <div className="mt-2 h-1.5 overflow-hidden rounded bg-[#1f2937]"><div className="h-full rounded bg-current" style={{width: `${Math.round(analysis.confidence*100)}%`}} /></div>
          <div className="mt-1 text-[10px] text-[#64748b]">Confidence {Math.round(analysis.confidence*100)}% · {analysis.regime}</div>
        </div>
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-2">
          <div className="mb-2 text-[10px] uppercase text-[#64748b]">Premium / Discount</div>
          {analysis.premiumDiscount ? <div className="grid grid-cols-3 gap-1 text-center text-[10px]"><span className={analysis.premiumDiscount.zone==="discount"?"rounded bg-[#172f2a] p-1.5 text-emerald-300":"rounded bg-[#121923] p-1.5"}>Discount</span><span className={analysis.premiumDiscount.zone==="equilibrium"?"rounded bg-[#27313d] p-1.5 text-white":"rounded bg-[#121923] p-1.5"}>EQ</span><span className={analysis.premiumDiscount.zone==="premium"?"rounded bg-[#382020] p-1.5 text-red-300":"rounded bg-[#121923] p-1.5"}>Premium</span></div> : <span className="text-xs text-[#64748b]">Insufficient range.</span>}
        </div>
        <div className="space-y-1.5">{analysis.mtf.map(m => <div key={m.timeframe} className="flex items-center justify-between rounded border border-[#263241] bg-[#0a0f16] px-3 py-2 text-xs"><span>{m.timeframe} · {m.structure}</span><span>{m.bias}</span></div>)}</div>
        {structurePoints.slice(-6).reverse().map(p => <div key={String(p.time)} className="flex justify-between rounded border border-[#263241] bg-[#0a0f16] px-3 py-2 text-xs"><span>{p.label} {p.high?"high":"low"}</span><span className="tabular-nums text-[#94a3b8]">{p.price.toFixed(5)}</span></div>)}
        {structureEvents.slice(-4).reverse().map((e,i) => <div key={String(e.time)+"-"+i} className="flex justify-between rounded border border-[#263241] bg-[#101722] px-3 py-2 text-xs"><span>{e.type}</span><span>{e.bullish?"Bullish":"Bearish"}</span></div>)}
      </div>}

      {tab === "intelligence" && <div className="space-y-3">
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">
          <div className="flex items-center justify-between"><span className="text-xs uppercase text-[#64748b]">Decision engine</span><strong>{analysis.recommendation.toUpperCase()}</strong></div>
          <div className="mt-1 text-[10px] text-[#64748b]">{biasLabel(analysis)} · score {analysis.score.toFixed(2)} · {Math.round(analysis.confidence*100)}% confidence</div><div className="mt-2 text-[10px] text-[#64748b]">Confluence {analysis.confluence.score}/{analysis.confluence.threshold}</div><div className="mt-1 grid grid-cols-2 gap-1">{analysis.confluence.gates.map(g=><span key={g.id} className={`rounded px-1.5 py-1 ${g.passed?"bg-[#173128] text-emerald-300":"bg-[#241b1b] text-red-300"}`}>{g.passed?"✓":"×"} {g.id.replaceAll("_"," ")}</span>)}</div>
        </div>
        <div className="grid grid-cols-2 gap-1.5">{analysis.modules.map(m => <div key={m.module} className="rounded border border-[#263241] bg-[#0a0f16] p-2"><div className="text-[10px] uppercase text-[#64748b]">{m.module.replace("_"," ")}</div><div className="mt-1 text-xs">{m.bias} · {Math.round(m.confidence*100)}%</div></div>)}</div>
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">
          <div className="mb-2 text-[10px] uppercase text-[#64748b]">Liquidity</div>
          {analysis.liquidity.pools.slice(-5).reverse().map((p,i) => <div key={String(p.start)+"-"+i} className="flex justify-between border-b border-[#1f2937] py-1.5 text-xs last:border-0"><span>{p.kind.replace("_"," ")} · {p.touches}x</span><span>{p.price.toFixed(5)} {p.swept?"· swept":""}</span></div>)}
          {analysis.liquidity.sweeps.slice(-3).reverse().map((s,i) => <div key={String(s.time)+"-"+i} className="mt-1 text-[10px] text-[#94a3b8]">{s.kind.replace("_"," ")} sweep · {s.reclaimed?"reclaimed":"breached"}</div>)}
          {!analysis.liquidity.pools.length && <div className="text-xs text-[#64748b]">No equal-high/low liquidity cluster.</div>}
        </div>
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-3"><div className="mb-2 text-[10px] uppercase text-[#64748b]">Displacement / order blocks</div>
          {analysis.displacement.slice(-3).reverse().map(d=><div key={String(d.time)} className="flex justify-between text-xs"><span>{d.bullish?"Bullish":"Bearish"} displacement</span><span>{d.atrMultiple.toFixed(1)}× ATR</span></div>)}
          {orderBlocks.slice(-4).reverse().map((b,i)=><div key={String(b.time)+"-"+i} className="mt-1 flex justify-between text-xs"><span>{b.bullish?"Bullish":"Bearish"} OB</span><span>{Math.round(b.strength*100)}%</span></div>)}
        </div>
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-3"><div className="mb-2 text-[10px] uppercase text-[#64748b]">Evidence</div>{analysis.evidence.slice(-6).map((e,i)=><div key={i} className="text-[10px] leading-4 text-[#94a3b8]">{e}</div>)}</div>
      </div>}

      {tab === "risk" && <div className="space-y-3">
        <div className="rounded border border-[#263241] bg-[#0a0f16] p-3"><div className="text-xs uppercase text-[#64748b]">Risk engine</div><div className="mt-2 text-sm">Account context required</div><div className="mt-1 text-[10px] leading-4 text-[#64748b]">Equity, leverage, broker contract size, entry, stop and target are required before a position size can be calculated.</div></div>
        {[["Equity","—"],["Leverage","—"],["Risk %","—"],["Entry","—"],["Stop loss","—"],["Take profit","—"],["R:R","—"]].map(([k,v])=><div key={k} className="flex justify-between border-b border-[#1f2937] pb-2 text-xs"><span className="text-[#778497]">{k}</span><span>{v}</span></div>)}
      </div>}

      {tab === "objects" && <div className="space-y-2 text-xs text-[#8b98aa]">
        {drawings.length === 0 && <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">No chart objects.</div>}
        {drawings.map((d,i)=><div key={d.id} onClick={()=>setSelectedDrawingId(d.id)} className={`rounded border p-2 ${selectedDrawingId===d.id?"border-[#fbbf24] bg-[#111a25]":"border-[#263241] bg-[#0a0f16]"}`}><div className="flex items-center justify-between"><span className="uppercase">{d.tool}</span><span className="text-[#64748b]">#{i+1}</span></div><div className="mt-2 flex gap-1"><button onClick={()=>setDrawings(drawings.map(x=>x.id===d.id?{...x,visible:x.visible===false}:x))} className="rounded border border-[#334155] px-2 py-1">{d.visible===false?"Show":"Hide"}</button><button onClick={()=>setDrawings(drawings.map(x=>x.id===d.id?{...x,locked:!x.locked}:x))} className="rounded border border-[#334155] px-2 py-1">{d.locked?"Unlock":"Lock"}</button><button onClick={()=>{setDrawings(drawings.filter(x=>x.id!==d.id)); if(selectedDrawingId===d.id)setSelectedDrawingId(null);}} className="rounded border border-[#334155] px-2 py-1 text-red-300">Delete</button></div></div>)}
      </div>}
    </div>
    <div className="border-t border-[#27313d] p-3"><label className="flex items-center justify-between text-xs"><span>{t(locale,"grid")}</span><input type="checkbox" checked={preferences.showGrid} onChange={e=>setPreferences({...preferences,showGrid:e.target.checked})}/></label><label className="mt-2 flex items-center justify-between text-xs"><span>{t(locale,"magnet")}</span><input type="checkbox" checked={preferences.magnet} onChange={e=>setPreferences({...preferences,magnet:e.target.checked})}/></label></div>
  </aside>;
}
