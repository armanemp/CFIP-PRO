"use client";

import { useEffect, useState } from "react";
import { ProfessionalChartTerminalV3 } from "@/components/professional-chart-terminal-v3";
import { getMarketObservations, getPlatformManifest, type MarketObservation } from "@/lib/api";

export function TerminalShell() {
  const [observations, setObservations] = useState<MarketObservation[]>([]);
  const [symbol, setSymbol] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const bootstrap = async () => {
      try {
        const manifest = await getPlatformManifest();
        const configuredSymbol = manifest.defaults.default_symbol;
        if (!active) return;
        setSymbol(configuredSymbol);
        const data = await getMarketObservations(configuredSymbol, "reference", 5000);
        if (active) setObservations(data);
      } catch (reason: unknown) {
        if (active) setError(reason instanceof Error ? reason.message : "Terminal bootstrap failed");
      }
    };
    void bootstrap();
    return () => { active = false; };
  }, []);

  if (!symbol) {
    return <main className="flex h-dvh items-center justify-center bg-[#080b10] text-xs text-[#748196]">
      {error ?? "Initializing terminal…"}
    </main>;
  }

  return <main className="flex h-dvh min-h-0 flex-col overflow-hidden bg-[#080b10] text-sm text-[#c9d2de]">
    <header className="flex h-10 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-3">
      <strong className="text-[13px] font-semibold tracking-[0.14em] text-white">CFIP-PRO</strong>
      <span className="mx-4 h-4 w-px bg-[#2b3542]" />
      <span className="text-xs text-[#748196]">CHART TERMINAL</span>
      <div className="ml-auto text-[10px] text-[#687689]">
        {error ? "DEGRADED" : "NORMALIZED MARKET DATA"}
      </div>
    </header>
    <section className="min-h-0 min-w-0 flex-1">
      <ProfessionalChartTerminalV3 observations={observations} symbol={symbol} />
    </section>
  </main>;
}
