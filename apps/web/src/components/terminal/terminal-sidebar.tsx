"use client";

import type {
  Candle,
  ChartPreferences,
  Drawing,
  InspectorTab,
  Locale,
  OrderBlock,
  StructureEvent,
  StructurePoint,
} from "./types";
import type { UnifiedAnalysis } from "./analysis-contracts";
import { t } from "./i18n";
import "./terminal-theme.module.css";
import { DEFAULT_WATCHLIST } from "./terminal-config";
import { RiskCalculator } from "./risk-calculator";

const tabsA: InspectorTab[] = ["market", "watchlist", "structure"];
const tabsB: InspectorTab[] = ["intelligence", "risk", "objects"];

function biasLabel(analysis: UnifiedAnalysis): string {
  if (analysis.bias === "bullish") return "Bullish";
  if (analysis.bias === "bearish") return "Bearish";
  return "Neutral";
}

export function TerminalSidebar({
  locale,
  tab,
  setTab,
  symbol,
  candles,
  collapsed,
  setCollapsed,
  preferences,
  setPreferences,
  drawings,
  setDrawings,
  selectedDrawingId,
  setSelectedDrawingId,
  structurePoints,
  structureEvents,
  orderBlocks,
  analysis,
  setSymbol,
}: {
  locale: Locale;
  tab: InspectorTab;
  setTab: (value: InspectorTab) => void;
  symbol: string;
  candles: Candle[];
  collapsed: boolean;
  setCollapsed: (value: boolean) => void;
  preferences: ChartPreferences;
  setPreferences: (value: ChartPreferences) => void;
  drawings: Drawing[];
  setDrawings: (value: Drawing[] | ((current: Drawing[]) => Drawing[])) => void;
  selectedDrawingId: string | null;
  setSelectedDrawingId: (id: string | null) => void;
  structurePoints: StructurePoint[];
  structureEvents: StructureEvent[];
  orderBlocks: OrderBlock[];
  analysis: UnifiedAnalysis;
  setSymbol: (value: string) => void;
}) {
  const last = candles.at(-1);
  const previous = candles.at(-2);
  const change = last && previous ? ((last.close - previous.close) / previous.close) * 100 : 0;

  if (collapsed) {
    return (
      <button
        type="button"
        onClick={() => setCollapsed(false)}
        aria-label={t(locale, "showSidebar")}
        title={t(locale, "showSidebar")}
        className="absolute right-2 top-2 z-30 rounded border border-[var(--cfip-terminal-border-strong)] bg-[var(--cfip-terminal-surface)] px-2 py-2 text-xs text-[var(--cfip-terminal-text)] shadow-lg"
      >
        ‹
      </button>
    );
  }

  const toggleDrawingVisibility = (id: string) => {
    setDrawings(current => current.map(item => item.id === id ? { ...item, visible: item.visible === false } : item));
  };
  const toggleDrawingLock = (id: string) => {
    setDrawings(current => current.map(item => item.id === id ? { ...item, locked: !item.locked } : item));
  };
  const deleteDrawing = (id: string) => {
    setDrawings(current => current.filter(item => item.id !== id));
    if (selectedDrawingId === id) setSelectedDrawingId(null);
  };

  return (
    <aside className="cfip-terminal-sidebar relative flex w-[300px] shrink-0 flex-col border-l border-[var(--cfip-terminal-border)] bg-[var(--cfip-terminal-surface)]">
      <button
        type="button"
        onClick={() => setCollapsed(true)}
        aria-label={t(locale, "hideSidebar")}
        title={t(locale, "hideSidebar")}
        className="absolute right-2 top-2 z-10 rounded border border-[var(--cfip-terminal-border-strong)] px-2 py-1 text-xs text-[var(--cfip-terminal-chart-text)] hover:text-white"
      >
        ›
      </button>

      <div className="grid grid-cols-3 border-b border-[var(--cfip-terminal-border)] p-1 pr-10">
        {tabsA.map(item => (
          <button key={item} type="button" onClick={() => setTab(item)} className={`rounded px-2 py-2 text-[11px] ${tab === item ? "bg-[var(--cfip-terminal-accent)] text-white" : "text-[var(--cfip-terminal-muted)]"}`}>
            {t(locale, item)}
          </button>
        ))}
      </div>
      <div className="grid grid-cols-3 border-b border-[var(--cfip-terminal-border)] p-1">
        {tabsB.map(item => (
          <button key={item} type="button" onClick={() => setTab(item)} className={`rounded px-2 py-2 text-[11px] ${tab === item ? "bg-[var(--cfip-terminal-accent)] text-white" : "text-[var(--cfip-terminal-muted)]"}`}>
            {t(locale, item)}
          </button>
        ))}
      </div>

      <div className="min-h-0 flex-1 overflow-auto p-3 text-sm">
        {tab === "market" && (
          <div className="space-y-3">
            <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-3">
              <div className="text-xs text-[var(--cfip-terminal-muted)]">{symbol}</div>
              <div className="mt-1 text-2xl font-semibold tabular-nums">{last ? last.close.toFixed(5) : "—"}</div>
              <div className={change >= 0 ? "text-emerald-400" : "text-red-400"}>
                {last ? `${change >= 0 ? "+" : ""}${change.toFixed(2)}%` : "—"}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-2">
              {[
                [t(locale, "open"), last?.open],
                [t(locale, "high"), last?.high],
                [t(locale, "low"), last?.low],
                [t(locale, "close"), last?.close],
                [t(locale, "volume"), last?.volume],
              ].map(([label, value]) => (
                <div key={String(label)} className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-2">
                  <div className="text-[10px] uppercase text-[var(--cfip-terminal-text-faint)]">{label}</div>
                  <div className="mt-1 tabular-nums">{typeof value === "number" ? value.toFixed(5) : "—"}</div>
                </div>
              ))}
            </div>
          </div>
        )}

        {tab === "watchlist" && (
          <div className="space-y-1.5">
            {DEFAULT_WATCHLIST.map(item => (
              <button
                key={item}
                type="button"
                onClick={() => setSymbol(item)}
                className={`flex min-h-9 w-full items-center justify-between rounded border px-3 py-2 text-left ${item === symbol ? "border-[var(--cfip-terminal-info)] bg-[var(--cfip-terminal-accent)] text-white" : "border-[var(--cfip-terminal-border-subtle)] hover:bg-[var(--cfip-terminal-surface-active)]"}`}
              >
                <span>{item}</span>
                <span className="text-xs text-[var(--cfip-terminal-text-faint)]">{item === symbol && last ? last.close.toFixed(5) : "—"}</span>
              </button>
            ))}
          </div>
        )}

        {tab === "structure" && (
          <div className="space-y-3">
            <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-3">
              <div className="flex items-center justify-between">
                <span className="text-xs uppercase text-[var(--cfip-terminal-text-faint)]">Unified bias</span>
                <span className="font-semibold">{biasLabel(analysis)}</span>
              </div>
              <div className="mt-2 h-1.5 overflow-hidden rounded bg-[var(--cfip-terminal-border-soft)]">
                <div className="h-full rounded bg-current" style={{ width: `${Math.round(analysis.confidence * 100)}%` }} />
              </div>
              <div className="mt-1 text-[10px] text-[var(--cfip-terminal-text-faint)]">Confidence {Math.round(analysis.confidence * 100)}% · {analysis.regime}</div>
            </div>

            <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-2">
              <div className="mb-2 text-[10px] uppercase text-[var(--cfip-terminal-text-faint)]">Premium / Discount</div>
              {analysis.premiumDiscount ? (
                <div className="grid grid-cols-3 gap-1 text-center text-[10px]">
                  <span className={analysis.premiumDiscount.zone === "discount" ? "rounded bg-[#172f2a] p-1.5 text-emerald-300" : "rounded bg-[#121923] p-1.5"}>Discount</span>
                  <span className={analysis.premiumDiscount.zone === "equilibrium" ? "rounded bg-[#27313d] p-1.5 text-white" : "rounded bg-[#121923] p-1.5"}>EQ</span>
                  <span className={analysis.premiumDiscount.zone === "premium" ? "rounded bg-[#382020] p-1.5 text-red-300" : "rounded bg-[#121923] p-1.5"}>Premium</span>
                </div>
              ) : (
                <span className="text-xs text-[var(--cfip-terminal-text-faint)]">Insufficient range.</span>
              )}
            </div>

            <div className="space-y-1.5">
              {analysis.mtf.map(item => (
                <div key={item.timeframe} className="flex items-center justify-between rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] px-3 py-2 text-xs">
                  <span>{item.timeframe} · {item.structure}</span>
                  <span>{item.bias}</span>
                </div>
              ))}
            </div>
            {structurePoints.slice(-6).reverse().map(point => (
              <div key={String(point.time)} className="flex justify-between rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] px-3 py-2 text-xs">
                <span>{point.label} {point.high ? "high" : "low"}</span>
                <span className="tabular-nums text-[var(--cfip-terminal-chart-text)]">{point.price.toFixed(5)}</span>
              </div>
            ))}
            {structureEvents.slice(-4).reverse().map((event, index) => (
              <div key={`${String(event.time)}-${index}`} className="flex justify-between rounded border border-[var(--cfip-terminal-border-subtle)] bg-[#101722] px-3 py-2 text-xs">
                <span>{event.type}</span>
                <span>{event.bullish ? "Bullish" : "Bearish"}</span>
              </div>
            ))}
          </div>
        )}

        {tab === "intelligence" && (
          <div className="space-y-3">
            <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-3">
              <div className="flex items-center justify-between"><span className="text-xs uppercase text-[var(--cfip-terminal-text-faint)]">Decision engine</span><strong>{analysis.recommendation.toUpperCase()}</strong></div>
              <div className="mt-1 text-[10px] text-[var(--cfip-terminal-text-faint)]">{biasLabel(analysis)} · score {analysis.score.toFixed(2)} · {Math.round(analysis.confidence * 100)}% confidence</div>
              <div className="mt-2 text-[10px] text-[var(--cfip-terminal-text-faint)]">Confluence {analysis.confluence.score}/{analysis.confluence.threshold}</div>
              <div className="mt-1 grid grid-cols-2 gap-1">
                {analysis.confluence.gates.map(gate => (
                  <span key={gate.id} className={`rounded px-1.5 py-1 ${gate.passed ? "bg-[#173128] text-emerald-300" : "bg-[#241b1b] text-red-300"}`}>
                    {gate.passed ? "✓" : "×"} {gate.id.replaceAll("_", " ")}
                  </span>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-1.5">
              {analysis.modules.map(module => (
                <div key={module.module} className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-2">
                  <div className="text-[10px] uppercase text-[var(--cfip-terminal-text-faint)]">{module.module.replace("_", " ")}</div>
                  <div className="mt-1 text-xs">{module.bias} · {Math.round(module.confidence * 100)}%</div>
                </div>
              ))}
            </div>

            <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-3">
              <div className="mb-2 text-[10px] uppercase text-[var(--cfip-terminal-text-faint)]">Liquidity</div>
              {analysis.liquidity.pools.slice(-5).reverse().map((pool, index) => (
                <div key={`${String(pool.start)}-${index}`} className="flex justify-between border-b border-[#1f2937] py-1.5 text-xs last:border-0">
                  <span>{pool.kind.replace("_", " ")} · {pool.touches}x</span>
                  <span>{pool.price.toFixed(5)} {pool.swept ? "· swept" : ""}</span>
                </div>
              ))}
              {analysis.liquidity.sweeps.slice(-3).reverse().map((sweep, index) => (
                <div key={`${String(sweep.time)}-${index}`} className="mt-1 text-[10px] text-[var(--cfip-terminal-chart-text)]">{sweep.kind.replace("_", " ")} sweep · {sweep.reclaimed ? "reclaimed" : "breached"}</div>
              ))}
              {!analysis.liquidity.pools.length && <div className="text-xs text-[var(--cfip-terminal-text-faint)]">No equal-high/low liquidity cluster.</div>}
            </div>

            <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-3">
              <div className="mb-2 text-[10px] uppercase text-[var(--cfip-terminal-text-faint)]">Displacement / order blocks</div>
              {analysis.displacement.slice(-3).reverse().map(item => (
                <div key={String(item.time)} className="flex justify-between text-xs"><span>{item.bullish ? "Bullish" : "Bearish"} displacement</span><span>{item.atrMultiple.toFixed(1)}× ATR</span></div>
              ))}
              {orderBlocks.slice(-4).reverse().map((block, index) => (
                <div key={`${String(block.time)}-${index}`} className="mt-1 flex justify-between text-xs"><span>{block.bullish ? "Bullish" : "Bearish"} OB</span><span>{Math.round(block.strength * 100)}%</span></div>
              ))}
            </div>

            <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-3">
              <div className="mb-2 text-[10px] uppercase text-[var(--cfip-terminal-text-faint)]">Evidence</div>
              {analysis.evidence.slice(-6).map((evidence, index) => <div key={index} className="text-[10px] leading-4 text-[var(--cfip-terminal-chart-text)]">{evidence}</div>)}
            </div>
          </div>
        )}

        {tab === "risk" && <RiskCalculator locale={locale} lastPrice={last?.close ?? 0} />}

        {tab === "objects" && (
          <div className="space-y-2 text-xs text-[#8b98aa]">
            {drawings.length === 0 && <div className="rounded border border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)] p-3">No chart objects.</div>}
            {drawings.map((drawing, index) => (
              <div
                key={drawing.id}
                onClick={() => setSelectedDrawingId(drawing.id)}
                className={`rounded border p-2 ${selectedDrawingId === drawing.id ? "border-[#fbbf24] bg-[var(--cfip-terminal-surface-active)]" : "border-[var(--cfip-terminal-border-subtle)] bg-[var(--cfip-terminal-bg)]"}`}
              >
                <div className="flex items-center justify-between"><span className="uppercase">{drawing.tool}</span><span className="text-[var(--cfip-terminal-text-faint)]">#{index + 1}</span></div>
                <div className="mt-2 flex gap-1">
                  <button type="button" onClick={event => { event.stopPropagation(); toggleDrawingVisibility(drawing.id); }} className="rounded border border-[var(--cfip-terminal-border-strong)] px-2 py-1">{drawing.visible === false ? "Show" : "Hide"}</button>
                  <button type="button" onClick={event => { event.stopPropagation(); toggleDrawingLock(drawing.id); }} className="rounded border border-[var(--cfip-terminal-border-strong)] px-2 py-1">{drawing.locked ? "Unlock" : "Lock"}</button>
                  <button type="button" onClick={event => { event.stopPropagation(); deleteDrawing(drawing.id); }} className="rounded border border-[var(--cfip-terminal-border-strong)] px-2 py-1 text-red-300">Delete</button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="border-t border-[var(--cfip-terminal-border)] p-3">
        <label className="flex items-center justify-between text-xs">
          <span>{t(locale, "grid")}</span>
          <input type="checkbox" checked={preferences.showGrid} onChange={event => setPreferences({ ...preferences, showGrid: event.target.checked })} />
        </label>
        <label className="mt-2 flex items-center justify-between text-xs">
          <span>{t(locale, "magnet")}</span>
          <input type="checkbox" checked={preferences.magnet} onChange={event => setPreferences({ ...preferences, magnet: event.target.checked })} />
        </label>
      </div>
    </aside>
  );
}
