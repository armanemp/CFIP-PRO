"use client";

import { useEffect, useState } from "react";

import { CfipChartTerminal } from "@/components/cfip-chart-terminal";
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
    <main className="flex h-dvh min-h-0 flex-col overflow-hidden bg-[#080b10]">
      <header className="flex h-8 shrink-0 items-center justify-between border-b border-[#1c2632] bg-[#0c1118] px-3 text-[10px]">
        <div className="flex items-center gap-3">
          <strong className="tracking-[0.16em] text-white">CFIP-PRO</strong>
          <span className="text-[#788596]">{SYMBOL} · {VENUE}</span>
        </div>
        <div className={error ? "text-red-400" : "text-emerald-400"}>{error ? "DATA OFFLINE" : "MARKET DATA READY"}</div>
      </header>
      <section className="min-h-0 min-w-0 flex-1">
        <CfipChartTerminal observations={observations} symbol={SYMBOL} />
      </section>
    </main>
  );
}
