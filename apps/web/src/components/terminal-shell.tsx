"use client";

import { useEffect, useState } from "react";

import { ProfessionalChartTerminal } from "@/components/professional-chart-terminal";
import { getMarketObservations, type MarketObservation } from "@/lib/api";

const SYMBOL = "EUR/USD";
const VENUE = "reference";

export function TerminalShell() {
  const [observations, setObservations] = useState<MarketObservation[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    getMarketObservations(SYMBOL, VENUE)
      .then((data) => {
        if (active) setObservations(data);
      })
      .catch((reason: unknown) => {
        if (active) setError(reason instanceof Error ? reason.message : "Market data unavailable");
      });
    return () => {
      active = false;
    };
  }, []);

  return (
    <main className="flex h-dvh min-h-0 flex-col overflow-hidden bg-[#080b10] text-sm text-[#c9d2de]">
      <header className="flex h-10 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-3">
        <div className="flex min-w-[220px] items-center gap-4">
          <strong className="text-[13px] font-semibold tracking-[0.14em] text-white">CFIP-PRO</strong>
          <span className="h-4 w-px bg-[#2b3542]" />
          <span className="font-medium text-[#dce4ee]">{SYMBOL}</span>
          <span className="text-xs text-[#748196]">{VENUE}</span>
        </div>
        <nav className="flex h-full items-center gap-1 text-xs text-[#9aa8ba]" aria-label="Terminal sections">
          <span className="border-b-2 border-[#70a7ff] px-3 py-[11px] text-white">Chart</span>
          <span className="px-3">Market</span>
          <span className="px-3">Structure</span>
          <span className="px-3">Intelligence</span>
          <span className="px-3">Risk</span>
        </nav>
        <div className="ml-auto flex items-center gap-3 text-xs">
          <span className={error ? "text-red-400" : "text-emerald-400"}>{error ? "DATA OFFLINE" : "MARKET DATA READY"}</span>
          <span className="text-[#5f6c7e]">●</span>
        </div>
      </header>
      <section className="min-h-0 min-w-0 flex-1">
        <ProfessionalChartTerminal observations={observations} symbol={SYMBOL} />
      </section>
    </main>
  );
}
