"use client";

import { useEffect, useMemo, useState } from "react";
import { getProviderCatalog, type ProviderDescriptor } from "@/lib/api";

const KINDS = ["all", "market-data", "broker", "ai", "research"] as const;
type KindFilter = (typeof KINDS)[number];

export function ProviderCatalogPanel() {
  const [providers, setProviders] = useState<ProviderDescriptor[]>([]);
  const [kind, setKind] = useState<KindFilter>("all");
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    getProviderCatalog().then(setProviders).catch((e: unknown) => setError(e instanceof Error ? e.message : "Provider catalog unavailable"));
  }, []);
  const filtered = useMemo(() => providers.filter(p => kind === "all" || p.kind === kind), [providers, kind]);
  return <section className="space-y-3">
    <div className="flex flex-wrap gap-1.5">
      {KINDS.map(item => <button key={item} type="button" onClick={() => setKind(item)} className={`rounded-md border px-2.5 py-1.5 text-[10px] uppercase tracking-wider ${kind === item ? "border-[#3b82f6] bg-[#142238] text-white" : "border-[#293748] text-[#718096] hover:text-white"}`}>{item}</button>)}
    </div>
    {error && <div className="rounded-lg border border-amber-900/50 bg-amber-950/20 p-3 text-xs text-amber-300">{error}</div>}
    <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-3">
      {filtered.map(provider => <article key={provider.id} className="rounded-lg border border-[#202d3c] bg-[#0a1017] p-3">
        <div className="flex items-start justify-between gap-2"><div><div className="text-sm font-medium text-white">{provider.name}</div><div className="mt-0.5 text-[10px] text-[#64748b]">{provider.id} · {provider.kind}</div></div><span className="rounded-full border border-[#293748] px-2 py-0.5 text-[9px] uppercase text-[#8391a4]">{provider.status}</span></div>
        <div className="mt-3 flex flex-wrap gap-1">{provider.capabilities.map(cap => <span key={cap} className="rounded bg-[#121b26] px-1.5 py-1 text-[9px] text-[#8c9aae]">{cap}</span>)}</div>
        <div className="mt-3 text-[10px] text-[#59687b]">{provider.credential_required ? "Credentials required" : "No credentials required"}</div>
      </article>)}
    </div>
  </section>;
}
