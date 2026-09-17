"use client";
import { useMemo, useState } from "react";
import { forexSymbols } from "./symbols";
import type { Locale, SymbolDefinition } from "./types";
import { t } from "./i18n";

export function SymbolPicker({ locale, value, onChange }: { locale: Locale; value: string; onChange: (symbol: SymbolDefinition) => void }) {
  const [query, setQuery] = useState("");
  const groups = useMemo(() => forexSymbols.filter(s => `${s.symbol} ${s.name}`.toLowerCase().includes(query.toLowerCase())), [query]);
  return <div className="absolute left-2 top-11 z-50 w-[360px] overflow-hidden rounded-lg border border-[#334155] bg-[#0d131b] shadow-2xl">
    <div className="border-b border-[#27313d] p-2"><input autoFocus value={query} onChange={e => setQuery(e.target.value)} placeholder={t(locale, "search")} className="w-full rounded border border-[#334155] bg-[#080b10] px-3 py-2 text-sm outline-none focus:border-[#70a7ff]" /></div>
    <div className="max-h-[420px] overflow-auto p-1">{groups.map(s => <button key={s.symbol} onClick={() => onChange(s)} className={`flex w-full items-center justify-between rounded px-3 py-2 text-left hover:bg-[#17202c] ${s.symbol === value ? "bg-[#16263a]" : ""}`}><span><strong>{s.symbol}</strong><span className="ml-2 text-xs text-[#778497]">{s.name}</span></span><span className="text-[10px] uppercase text-[#5e6b7d]">{s.category}</span></button>)}{groups.length === 0 && <div className="p-5 text-center text-xs text-[#718096]">No symbols</div>}</div>
  </div>;
}
