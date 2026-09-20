import type { ChartKind, ChartPreferences, Drawing, InspectorTab, Locale, ReplayState, Timeframe, Tool } from "./types";

/** Canonical terminal workspace state shared by UI modules and persistence. */
export interface TerminalWorkspaceState {
  symbol: string;
  timeframe: Timeframe;
  chartKind: ChartKind;
  locale: Locale;
  tool: Tool;
  inspectorTab: InspectorTab;
  openPanel: string | null;
  selectedStudies: string[];
  preferences: ChartPreferences;
  drawings: Drawing[];
  selectedDrawingId: string | null;
  pendingPoint: Drawing["a"] | null;
  replay: ReplayState;
}

export type TerminalWorkspaceAction =
  | { type: "symbol/set"; symbol: string }
  | { type: "timeframe/set"; timeframe: Timeframe }
  | { type: "chart-kind/set"; chartKind: ChartKind }
  | { type: "locale/set"; locale: Locale }
  | { type: "tool/set"; tool: Tool }
  | { type: "inspector/set"; tab: InspectorTab }
  | { type: "panel/toggle"; panel: string | null }
  | { type: "studies/toggle"; id: string }
  | { type: "preferences/patch"; patch: Partial<ChartPreferences> }
  | { type: "drawings/set"; drawings: Drawing[] }
  | { type: "drawing/select"; id: string | null }
  | { type: "drawing/pending"; point: Drawing["a"] | null }
  | { type: "replay/set"; replay: ReplayState };

export function terminalWorkspaceReducer(
  state: TerminalWorkspaceState,
  action: TerminalWorkspaceAction,
): TerminalWorkspaceState {
  switch (action.type) {
    case "symbol/set": return { ...state, symbol: action.symbol };
    case "timeframe/set": return { ...state, timeframe: action.timeframe };
    case "chart-kind/set": return { ...state, chartKind: action.chartKind };
    case "locale/set": return { ...state, locale: action.locale };
    case "tool/set": return { ...state, tool: action.tool };
    case "inspector/set": return { ...state, inspectorTab: action.tab };
    case "panel/toggle": return { ...state, openPanel: action.panel };
    case "studies/toggle":
      return {
        ...state,
        selectedStudies: state.selectedStudies.includes(action.id)
          ? state.selectedStudies.filter(id => id !== action.id)
          : [...state.selectedStudies, action.id],
      };
    case "preferences/patch":
      return { ...state, preferences: { ...state.preferences, ...action.patch } };
    case "drawings/set": return { ...state, drawings: action.drawings };
    case "drawing/select": return { ...state, selectedDrawingId: action.id };
    case "drawing/pending": return { ...state, pendingPoint: action.point };
    case "replay/set": return { ...state, replay: action.replay };
  }
}

export function createTerminalWorkspaceState(input: Pick<TerminalWorkspaceState, "symbol" | "preferences" | "replay">): TerminalWorkspaceState {
  return {
    symbol: input.symbol,
    timeframe: "1m",
    chartKind: "candles",
    locale: "en",
    tool: "cursor",
    inspectorTab: "market",
    openPanel: null,
    selectedStudies: ["EMA20"],
    preferences: input.preferences,
    drawings: [],
    selectedDrawingId: null,
    pendingPoint: null,
    replay: input.replay,
  };
}
