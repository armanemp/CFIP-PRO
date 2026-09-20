import type { ChartKind, ChartPreferences, Drawing, Locale, Timeframe, Tool } from "./types";
import { CHART_KINDS, LOCALES, TIMEFRAMES } from "./terminal-config";
import { INDICATOR_IDS } from "./indicator-registry";

export interface TerminalSession {
  symbol: string;
  timeframe: Timeframe;
  chartKind: ChartKind;
  locale: Locale;
  tool: Tool;
  selectedStudies: string[];
  indicatorParameters: Record<string, Record<string, number>>;
  preferences: ChartPreferences;
  drawings: Drawing[];
}


const isOneOf = <T extends string>(values: readonly T[], value: unknown): value is T => typeof value === "string" && values.includes(value as T);
const isChartKind = (value: unknown): value is ChartKind => isOneOf(CHART_KINDS, value);
const isLocale = (value: unknown): value is Locale => isOneOf(LOCALES, value);
const isTimeframe = (value: unknown): value is Timeframe => isOneOf(TIMEFRAMES, value);
const isTool = (value: unknown): value is Tool => typeof value === "string" && ["cursor","crosshair","trendline","ray","horizontal","vertical","rectangle","fib","measure","long","short"].includes(value);

const KEY = "cfip-pro:terminal-session:v1";

export function loadTerminalSession(fallback: TerminalSession): TerminalSession {
  if (typeof window === "undefined") return fallback;
  try {
    const raw = window.localStorage.getItem(KEY);
    if (!raw) return fallback;
    const value = JSON.parse(raw) as Partial<TerminalSession>;
    const selectedStudies = Array.isArray(value.selectedStudies)
      ? value.selectedStudies.filter((id): id is string => typeof id === "string" && INDICATOR_IDS.includes(id as typeof INDICATOR_IDS[number]))
      : fallback.selectedStudies;
    return {
      ...fallback,
      ...value,
      symbol: typeof value.symbol === "string" && value.symbol.trim() ? value.symbol : fallback.symbol,
      timeframe: isTimeframe(value.timeframe) ? value.timeframe : fallback.timeframe,
      chartKind: isChartKind(value.chartKind) ? value.chartKind : fallback.chartKind,
      locale: isLocale(value.locale) ? value.locale : fallback.locale,
      tool: isTool(value.tool) ? value.tool : fallback.tool,
      selectedStudies: selectedStudies.length ? selectedStudies : fallback.selectedStudies,
      indicatorParameters: value.indicatorParameters && typeof value.indicatorParameters === "object" ? value.indicatorParameters : fallback.indicatorParameters,
      preferences: { ...fallback.preferences, ...(value.preferences ?? {}) },
      drawings: Array.isArray(value.drawings) ? value.drawings : fallback.drawings,
    };
  } catch {
    return fallback;
  }
}

export function saveTerminalSession(session: TerminalSession): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(KEY, JSON.stringify(session));
  } catch {
    // Storage can be unavailable in privacy-restricted browser contexts.
  }
}

export function clearTerminalSession(): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.removeItem(KEY);
  } catch {
    // Ignore storage failures; the in-memory terminal remains usable.
  }
}
