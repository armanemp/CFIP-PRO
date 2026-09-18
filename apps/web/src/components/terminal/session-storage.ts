import type { ChartKind, ChartPreferences, Drawing, Locale, Timeframe, Tool } from "./types";

export interface TerminalSession {
  symbol: string;
  timeframe: Timeframe;
  chartKind: ChartKind;
  locale: Locale;
  tool: Tool;
  selectedStudies: string[];
  preferences: ChartPreferences;
  drawings: Drawing[];
}

const KEY = "cfip-pro:terminal-session:v1";

export function loadTerminalSession(fallback: TerminalSession): TerminalSession {
  if (typeof window === "undefined") return fallback;
  try {
    const raw = window.localStorage.getItem(KEY);
    if (!raw) return fallback;
    const value = JSON.parse(raw) as Partial<TerminalSession>;
    return {
      ...fallback,
      ...value,
      selectedStudies: Array.isArray(value.selectedStudies) ? value.selectedStudies : fallback.selectedStudies,
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
