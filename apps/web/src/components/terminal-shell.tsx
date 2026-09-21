"use client";

import { useEffect, useState } from "react";
import { ProfessionalChartTerminalV3 } from "@/components/professional-chart-terminal-v3";
import { getMarketObservations, type MarketObservation } from "@/lib/api";
import { TERMINAL_DATA_DEFAULTS } from "@/components/terminal/terminal-config";
import { useEffect } from "react";

export function TerminalShell() {
  const [observations, setObservations] = useState<MarketObservation[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [identityName, setIdentityName] = useState("Market Intelligence");
  useEffect(() => {
    const handler = (event: Event) => {
      const detail = (event as CustomEvent<{ name?: string }>).detail;
      if (detail?.name) setIdentityName(detail.name);
    };
    window.addEventListener("platform-identity-change", handler);
    return () => window.removeEventListener("platform-identity-change", handler);
  }, []);

  useEffect(() => {
    let active = true;
    getMarketObservations(TERMINAL_DATA_DEFAULTS.symbol, TERMINAL_DATA_DEFAULTS.venue, TERMINAL_DATA_DEFAULTS.initialObservationLimit).then((data) => {
      if (active) setObservations(data);
    }).catch((reason: unknown) => {
      if (active) setError(reason instanceof Error ? reason.message : "Market data unavailable");
    });
    return () => { active = false; };
  }, []);

  return <main className="flex h-dvh min-h-0 flex-col overflow-hidden bg-[#080b10] text-sm text-[#c9d2de]">
    <header className="flex h-10 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-3">
      <strong className="text-[13px] font-semibold tracking-[0.14em] text-white">{identityName}</strong>
      <span className="mx-4 h-4 w-px bg-[#2b3542]" /><span className="text-xs text-[#748196]">CHART TERMINAL</span>
      <div className="ml-auto text-[10px] text-[#687689]">{error ? "DATA SOURCE UNAVAILABLE" : "NORMALIZED MARKET DATA"}</div>
    </header>
    <section className="min-h-0 min-w-0 flex-1"><ProfessionalChartTerminalV3 observations={observations} symbol={TERMINAL_DATA_DEFAULTS.symbol} /></section>
  </main>;
}
