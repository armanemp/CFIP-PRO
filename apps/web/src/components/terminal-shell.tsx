"use client";

import { useEffect, useState } from "react";
import { ProfessionalChartTerminalV3 } from "@/components/professional-chart-terminal-v3";
import { getMarketObservations, getTerminalManifest, type MarketObservation, type TerminalManifest } from "@/lib/api";

export function TerminalShell() {
  const [observations, setObservations] = useState<MarketObservation[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [manifest, setManifest] = useState<TerminalManifest | null>(null);

  useEffect(() => {
    let active = true;
    getTerminalManifest().then((next) => {
      if (!active) return;
      setManifest(next);
      return getMarketObservations(
        next.datafeed_defaults.symbol,
        next.datafeed_defaults.venue,
        next.datafeed_defaults.limit,
      );
    }).then((data) => {
      if (active && data) setObservations(data);
    }).catch((reason: unknown) => {
      if (active) setError(reason instanceof Error ? reason.message : "Terminal manifest or market data unavailable");
    });
    return () => { active = false; };
  }, []);

  return <main className="flex h-dvh min-h-0 flex-col overflow-hidden bg-[#080b10] text-sm text-[#c9d2de]">
    <header className="flex h-10 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-3">
      <strong className="text-[13px] font-semibold tracking-[0.14em] text-white">CFIP-PRO</strong>
      <span className="mx-4 h-4 w-px bg-[#2b3542]" /><span className="text-xs text-[#748196]">CHART TERMINAL</span>
      <div className="ml-auto text-[10px] text-[#687689]">{error ? "DATA SOURCE UNAVAILABLE" : "NORMALIZED MARKET DATA"}</div>
    </header>
    <section className="min-h-0 min-w-0 flex-1">{manifest ? <ProfessionalChartTerminalV3 observations={observations} symbol={manifest.datafeed_defaults.symbol} /> : <div className="grid h-full place-items-center text-xs text-[#687689]">Loading terminal configuration…</div>}</section>
  </main>;
}
