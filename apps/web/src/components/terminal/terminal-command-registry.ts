import type { Tool, Timeframe } from "./types";

export type CommandAction =
  | { type: "tool"; value: Tool }
  | { type: "timeframe"; value: Timeframe }
  | { type: "panel"; value: "symbol" | "timeframe" | "chartType" | "indicators" | "settings" | "language" }
  | { type: "navigate"; value: "/terminal" | "/admin" };

export interface TerminalCommand {
  id: string;
  label: string;
  keywords: readonly string[];
  action: CommandAction;
}

export const TERMINAL_COMMANDS: readonly TerminalCommand[] = [
  { id: "tool.cursor", label: "Cursor", keywords: ["cursor","select"], action: { type: "tool", value: "cursor" } },
  { id: "tool.crosshair", label: "Crosshair", keywords: ["crosshair","inspect"], action: { type: "tool", value: "crosshair" } },
  { id: "tool.trendline", label: "Trend line", keywords: ["trend","line"], action: { type: "tool", value: "trendline" } },
  { id: "tool.fib", label: "Fibonacci", keywords: ["fib","fibonacci"], action: { type: "tool", value: "fib" } },
  { id: "tool.measure", label: "Measure", keywords: ["measure","distance"], action: { type: "tool", value: "measure" } },
  { id: "tool.long", label: "Long position", keywords: ["long","risk"], action: { type: "tool", value: "long" } },
  { id: "tool.short", label: "Short position", keywords: ["short","risk"], action: { type: "tool", value: "short" } },
  { id: "tf.1m", label: "1 minute", keywords: ["1m","minute"], action: { type: "timeframe", value: "1m" } },
  { id: "tf.5m", label: "5 minutes", keywords: ["5m","minute"], action: { type: "timeframe", value: "5m" } },
  { id: "tf.15m", label: "15 minutes", keywords: ["15m","minute"], action: { type: "timeframe", value: "15m" } },
  { id: "tf.1h", label: "1 hour", keywords: ["1h","hour"], action: { type: "timeframe", value: "1H" } },
  { id: "panel.indicators", label: "Indicators", keywords: ["indicator","study"], action: { type: "panel", value: "indicators" } },
  { id: "panel.settings", label: "Chart settings", keywords: ["settings","preferences"], action: { type: "panel", value: "settings" } },
  { id: "nav.admin", label: "Admin control plane", keywords: ["admin","control"], action: { type: "navigate", value: "/admin" } },
];

export function searchTerminalCommands(query: string): TerminalCommand[] {
  const needle = query.trim().toLowerCase();
  if (!needle) return [...TERMINAL_COMMANDS];
  return TERMINAL_COMMANDS.filter(command => [command.label, ...command.keywords].some(value => value.toLowerCase().includes(needle)));
}
