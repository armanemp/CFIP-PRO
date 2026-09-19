import type { Tool, Timeframe } from "./types";

export type CommandAction =
  | { type: "tool"; value: Tool }
  | { type: "timeframe"; value: Timeframe }
  | { type: "panel"; value: "symbol" | "timeframe" | "chartType" | "indicators" | "settings" | "language" }
  | { type: "navigate"; value: "/terminal" | "/admin" }
  | { type: "system"; value: "fullscreen" | "reset-view" | "screenshot" | "command-palette" }
  | { type: "terminal"; value: "replay" | "alerts" | "objects" | "watchlist" | "depth" | "time-sales" | "compare" | "templates" | "screener" | "paper-trading" | "orders" | "positions" | "portfolio" | "journal" };

export interface TerminalCommand {
  id: string;
  label: string;
  keywords: readonly string[];
  action: CommandAction;
}

export const TERMINAL_COMMANDS: readonly TerminalCommand[] = [
  { id: "tool.cursor", label: "Cursor", keywords: ["cursor", "select"], action: { type: "tool", value: "cursor" } },
  { id: "tool.crosshair", label: "Crosshair", keywords: ["crosshair", "inspect"], action: { type: "tool", value: "crosshair" } },
  { id: "tool.trendline", label: "Trend line", keywords: ["trend", "line"], action: { type: "tool", value: "trendline" } },
  { id: "tool.fib", label: "Fibonacci", keywords: ["fib", "fibonacci"], action: { type: "tool", value: "fib" } },
  { id: "tool.measure", label: "Measure", keywords: ["measure", "distance"], action: { type: "tool", value: "measure" } },
  { id: "tool.long", label: "Long position", keywords: ["long", "risk"], action: { type: "tool", value: "long" } },
  { id: "tool.short", label: "Short position", keywords: ["short", "risk"], action: { type: "tool", value: "short" } },
  { id: "tf.1m", label: "1 minute", keywords: ["1m", "minute"], action: { type: "timeframe", value: "1m" } },
  { id: "tf.5m", label: "5 minutes", keywords: ["5m", "minute"], action: { type: "timeframe", value: "5m" } },
  { id: "tf.15m", label: "15 minutes", keywords: ["15m", "minute"], action: { type: "timeframe", value: "15m" } },
  { id: "tf.30m", label: "30 minutes", keywords: ["30m", "minute"], action: { type: "timeframe", value: "30m" } },
  { id: "tf.1h", label: "1 hour", keywords: ["1h", "hour"], action: { type: "timeframe", value: "1H" } },
  { id: "tf.4h", label: "4 hours", keywords: ["4h", "hour"], action: { type: "timeframe", value: "4H" } },
  { id: "tf.1d", label: "1 day", keywords: ["1d", "day"], action: { type: "timeframe", value: "1D" } },
  { id: "tf.1w", label: "1 week", keywords: ["1w", "week"], action: { type: "timeframe", value: "1W" } },
  { id: "tf.1mth", label: "1 month", keywords: ["1mth", "month"], action: { type: "timeframe", value: "1M" } },
  { id: "panel.indicators", label: "Indicators", keywords: ["indicator", "study"], action: { type: "panel", value: "indicators" } },
  { id: "panel.settings", label: "Chart settings", keywords: ["settings", "preferences"], action: { type: "panel", value: "settings" } },
  { id: "panel.language", label: "Language", keywords: ["language", "locale", "rtl"], action: { type: "panel", value: "language" } },
  { id: "nav.admin", label: "Admin control plane", keywords: ["admin", "control"], action: { type: "navigate", value: "/admin" } },
  { id: "system.fullscreen", label: "Fullscreen chart", keywords: ["fullscreen", "full", "screen"], action: { type: "system", value: "fullscreen" } },
  { id: "system.reset-view", label: "Reset chart view", keywords: ["reset", "view", "fit"], action: { type: "system", value: "reset-view" } },
  { id: "system.screenshot", label: "Capture chart", keywords: ["screenshot", "capture", "export"], action: { type: "system", value: "screenshot" } },
  { id: "terminal.replay", label: "Bar Replay", keywords: ["replay", "history", "backtest"], action: { type: "terminal", value: "replay" } },
  { id: "terminal.alerts", label: "Alerts", keywords: ["alert", "notification", "price"], action: { type: "terminal", value: "alerts" } },
  { id: "terminal.objects", label: "Object manager", keywords: ["objects", "drawings", "studies"], action: { type: "terminal", value: "objects" } },
  { id: "terminal.watchlist", label: "Watchlist", keywords: ["watchlist", "symbols"], action: { type: "terminal", value: "watchlist" } },
  { id: "terminal.depth", label: "Market depth", keywords: ["depth", "dom", "orderbook"], action: { type: "terminal", value: "depth" } },
  { id: "terminal.time-sales", label: "Time & Sales", keywords: ["time", "sales", "tape"], action: { type: "terminal", value: "time-sales" } },
  { id: "terminal.compare", label: "Compare symbols", keywords: ["compare", "overlay", "symbols"], action: { type: "terminal", value: "compare" } },
  { id: "terminal.templates", label: "Chart templates", keywords: ["template", "layout", "workspace"], action: { type: "terminal", value: "templates" } },
  { id: "terminal.screener", label: "Market screener", keywords: ["screener", "scan", "filter"], action: { type: "terminal", value: "screener" } },
  { id: "terminal.paper-trading", label: "Paper trading", keywords: ["paper", "simulation", "practice"], action: { type: "terminal", value: "paper-trading" } },
  { id: "terminal.orders", label: "Orders", keywords: ["orders", "order", "entry"], action: { type: "terminal", value: "orders" } },
  { id: "terminal.positions", label: "Positions", keywords: ["positions", "open", "trades"], action: { type: "terminal", value: "positions" } },
  { id: "terminal.portfolio", label: "Portfolio", keywords: ["portfolio", "exposure", "pnl"], action: { type: "terminal", value: "portfolio" } },
  { id: "terminal.journal", label: "Trading journal", keywords: ["journal", "notes", "review"], action: { type: "terminal", value: "journal" } },
];

export function searchTerminalCommands(query: string): TerminalCommand[] {
  const needle = query.trim().toLowerCase();
  if (!needle) return [...TERMINAL_COMMANDS];
  return TERMINAL_COMMANDS.filter(command =>
    [command.label, ...command.keywords].some(value => value.toLowerCase().includes(needle)),
  );
}
