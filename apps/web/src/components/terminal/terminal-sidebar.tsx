"use client";
import type { Candle, Drawing, InspectorTab, Locale, ChartPreferences } from "./types";
import { t } from "./i18n";

export function TerminalSidebar({ locale, tab, setTab, symbol, candles, collapsed, setCollapsed, preferences, setPreferences, drawings, setDrawings }: { locale: Locale; tab: InspectorTab; setTab: (v: InspectorTab) => void; symbol: string; candles: Candle[]; collapsed: boolean; setCollapsed: (v: boolean) => void; preferences: ChartPreferences; setPreferences: (v: ChartPreferences) => void; drawings: Drawing[]; setDrawings: (v: Drawing[]) => void }) {
  const last = candles.at(-1); const prev = candles.at(-2); const change = last && prev ? ((last.close - prev.close) / prev.close) * 100 : 0;
  if (collapsed) return <button onClick={() => setCollapsed(false)} aria-label={t(locale, "showSidebar")} title={t(locale, "showSidebar")} className="absolute right-2 top-2 z-30 rounded border border-[#334155] bg-[#0d131b] px-2 py-2 text-xs text-[#c8d2df] shadow-lg">‹</button>;
  return <aside className="relative flex w-[300px] shrink-0 flex-col border-l border-[#27313d] bg-[#0d131b]">
    <button onClick={() => setCollapsed(true)} aria-label={t(locale, "hideSidebar")} title={t(locale, "hideSidebar")} className="absolute right-2 top-2 z-10 rounded border border-[#334155] px-2 py-1 text-xs text-[#94a3b8] hover:text-white">›</button>
    <div className="grid grid-cols-3 border-b border-[#27313d] p-1 pr-10">{(["market", "watchlist", "structure"] as InspectorTab[]).map(x => <button key={x} onClick={() => setTab(x)} className={`rounded px-2 py-2 text-[11px] ${tab === x ? "bg-[#1b2b3f] text-white" : "text-[#718096]"}`}>{t(locale, x)}</button>)}</div>
    <div className="grid grid-cols-3 border-b border-[#27313d] p-1">{(["intelligence", "risk", "objects"] as InspectorTab[]).map(x => <button key={x} onClick={() => setTab(x)} className={`rounded px-2 py-2 text-[11px] ${tab === x ? "bg-[#1b2b3f] text-white" : "text-[#718096]"}`}>{t(locale, x)}</button>)}</div>
    <div className="min-h-0 flex-1 overflow-auto p-4 text-sm">
      {tab === "market" && <div className="space-y-4"><div><div className="text-xs text-[#718096]">{symbol}</div><div className="mt-1 text-2xl font-semibold tabular-nums">{last ? last.close.toFixed(5) : "—"}</div><div className={change >= 0 ? "text-emerald-400" : "text-red-400"}>{last ? `${change >= 0 ? "+" : ""}${change.toFixed(2)}%` : "—"}</div></div><div className="grid grid-cols-2 gap-2">{[[t(locale,"open"),last?.open],[t(locale,"high"),last?.high],[t(locale,"low"),last?.low],[t(locale,"close"),last?.close],[t(locale,"volume"),last?.volume]].map(([k,v])=><div key={String(k)} className="rounded border border-[#263241] bg-[#0a0f16] p-2"><div className="text-[10px] uppercase text-[#64748b]">{k}</div><div className="mt-1 tabular-nums">{typeof v === "number" ? v.toFixed(5) : "—"}</div></div>)}</div></div>}
      {tab === "watchlist" && <div className="space-y-2">{["EUR/USD","GBP/USD","USD/JPY","USD/CHF","AUD/USD","USD/CAD","NZD/USD","EUR/JPY"].map(s=><div key={s} className="flex items-center justify-between rounded border border-[#263241] px-3 py-2"><span>{s}</span><span className="text-xs text-[#64748b]">—</span></div>)}</div>}
      {tab === "structure" && <div className="space-y-2">{["HH / HL / LH / LL","BOS","CHoCH","MSS","Equal highs / lows","Support / resistance","Premium / discount"].map(x=><div key={x} className="rounded border border-[#263241] bg-[#0a0f16] px-3 py-2">{x}<span className="float-right text-xs text-[#64748b]">analysis</span></div>)}</div>}
      {tab === "intelligence" && <div className="space-y-2">{["FVG / IFVG","Order Block","Breaker Block","Mitigation Block","Liquidity sweep","Displacement","Session ranges","Kill zones"].map(x=><div key={x} className="rounded border border-[#263241] bg-[#0a0f16] px-3 py-2">{x}<span className="float-right text-xs text-[#64748b]">ready</span></div>)}</div>}
      {tab === "risk" && <div className="space-y-3">{[["Equity","—"],["Leverage","—"],["Risk %","—"],["Entry","—"],["Stop loss","—"],["Take profit","—"],["R:R","—"]].map(([k,v])=><div key={k} className="flex justify-between border-b border-[#1f2937] pb-2"><span className="text-[#778497]">{k}</span><span>{v}</span></div>)}<p className="text-xs leading-5 text-[#64748b]">Risk calculations stay account-aware and are intentionally not fabricated until a broker/account context is connected.</p></div>}
      {tab === "objects" && <div className="space-y-2 text-xs text-[#8b98aa]">
        {drawings.length === 0 && <div className="rounded border border-[#263241] bg-[#0a0f16] p-3">No chart objects.</div>}
        {drawings.map((d,i)=><div key={d.id} className="rounded border border-[#263241] bg-[#0a0f16] p-2">
          <div className="flex items-center justify-between"><span className="uppercase">{d.tool}</span><span className="text-[#64748b]">#{i+1}</span></div>
          <div className="mt-2 flex gap-1">
            <button onClick={()=>setDrawings(drawings.map(x=>x.id===d.id?{...x,visible:x.visible===false}:x))} className="rounded border border-[#334155] px-2 py-1">{d.visible===false?"Show":"Hide"}</button>
            <button onClick={()=>setDrawings(drawings.map(x=>x.id===d.id?{...x,locked:!x.locked}:x))} className="rounded border border-[#334155] px-2 py-1">{d.locked?"Unlock":"Lock"}</button>
            <button onClick={()=>setDrawings(drawings.filter(x=>x.id!==d.id))} className="rounded border border-[#334155] px-2 py-1 text-red-300">Delete</button>
          </div>
        </div>)}
      </div>}
    </div>
    <div className="border-t border-[#27313d] p-3"><label className="flex items-center justify-between text-xs"><span>{t(locale,"grid")}</span><input type="checkbox" checked={preferences.showGrid} onChange={e=>setPreferences({...preferences,showGrid:e.target.checked})}/></label><label className="mt-2 flex items-center justify-between text-xs"><span>{t(locale,"magnet")}</span><input type="checkbox" checked={preferences.magnet} onChange={e=>setPreferences({...preferences,magnet:e.target.checked})}/></label></div>
  </aside>;
}
