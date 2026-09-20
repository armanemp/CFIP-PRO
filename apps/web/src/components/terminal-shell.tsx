"use client";

import { ProfessionalChartTerminalV3 } from "@/components/professional-chart-terminal-v3";
import { TERMINAL_DATA_DEFAULTS } from "@/components/terminal/terminal-config";

export function TerminalShell() {
  return <main className="flex h-dvh min-h-0 flex-col overflow-hidden bg-[#080b10] text-sm text-[#c9d2de]">
    <header className="flex h-10 shrink-0 items-center border-b border-[#27313d] bg-[#0d131b] px-3">
      <strong className="text-[13px] font-semibold tracking-[0.14em] text-white">CFIP-PRO</strong>
      <span className="mx-4 h-4 w-px bg-[#2b3542]" /><span className="text-xs text-[#748196]">CHART TERMINAL</span>
      <div className="ml-auto text-[10px] text-[#687689]">NORMALIZED MARKET DATA</div>
    </header>
    <section className="min-h-0 min-w-0 flex-1">
      <ProfessionalChartTerminalV3 observations={[]} symbol={TERMINAL_DATA_DEFAULTS.symbol} />
    </section>
  </main>;
}
