import { MarketChart } from "@/components/market-chart";

const tools = ["Crosshair", "Trendline", "FVG", "Order Block", "Structure"];

export function TerminalShell() {
  return (
    <main className="flex h-dvh min-h-0 flex-col overflow-hidden bg-[var(--terminal-bg)]">
      <header className="flex h-12 shrink-0 items-center justify-between border-b border-[var(--terminal-border)] bg-[var(--terminal-panel)] px-4">
        <div className="flex items-center gap-4">
          <strong className="tracking-wide">CFIP-PRO</strong>
          <span className="text-xs text-[var(--terminal-muted)]">EUR/USD · 1H</span>
        </div>
        <div className="text-xs text-emerald-400">FOUNDATION ONLINE</div>
      </header>

      <section className="flex min-h-0 flex-1">
        <aside className="flex w-12 shrink-0 flex-col items-center gap-2 border-r border-[var(--terminal-border)] bg-[var(--terminal-panel)] py-3">
          {tools.map((tool) => (
            <button key={tool} title={tool} className="h-9 w-9 rounded border border-transparent text-[10px] text-[var(--terminal-muted)] hover:border-[var(--terminal-border)] hover:text-white">
              {tool.slice(0, 2).toUpperCase()}
            </button>
          ))}
        </aside>

        <section className="min-h-0 min-w-0 flex-1">
          <MarketChart />
        </section>

        <aside className="hidden w-72 shrink-0 border-l border-[var(--terminal-border)] bg-[var(--terminal-panel)] p-4 lg:block">
          <div className="mb-4 text-xs uppercase tracking-wider text-[var(--terminal-muted)]">Inspector</div>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between"><span>Instrument</span><span>EUR/USD</span></div>
            <div className="flex justify-between"><span>Timeframe</span><span>1H</span></div>
            <div className="flex justify-between"><span>Mode</span><span>Foundation</span></div>
          </div>
        </aside>
      </section>

      <footer className="flex h-10 shrink-0 items-center justify-between border-t border-[var(--terminal-border)] bg-[var(--terminal-panel)] px-4 text-xs text-[var(--terminal-muted)]">
        <span>Market data: adapter boundary ready</span>
        <span>Realtime: contract-first</span>
      </footer>
    </main>
  );
}
