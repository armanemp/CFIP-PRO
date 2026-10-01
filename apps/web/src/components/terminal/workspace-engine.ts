import type { ChartKind, ChartPreferences, Locale, Timeframe } from "./types";

export interface ChartWorkspace {
  id: string;
  name: string;
  symbols: string[];
  timeframe: Timeframe;
  chartKind: ChartKind;
  locale: Locale;
  preferences: ChartPreferences;
  indicators: string[];
}

export const DEFAULT_WORKSPACES: readonly ChartWorkspace[] = [
  { id:"trader", name:"Trader", symbols:["EUR/USD"], timeframe:"15m", chartKind:"candles", locale:"en", preferences:{showGrid:true,showVolume:true,showSessions:true,showBidAsk:true,magnet:true,rightSidebar:true,leftRail:true}, indicators:["EMA20","EMA50","RSI14"] },
  { id:"structure", name:"Structure", symbols:["EUR/USD"], timeframe:"1H", chartKind:"candles", locale:"en", preferences:{showGrid:true,showVolume:true,showSessions:true,showBidAsk:false,magnet:true,rightSidebar:true,leftRail:true}, indicators:["EMA50","EMA200","DONCHIAN20","RSI14","DMI14"] },
  { id:"volatility", name:"Volatility", symbols:["EUR/USD"], timeframe:"15m", chartKind:"candles", locale:"en", preferences:{showGrid:true,showVolume:true,showSessions:false,showBidAsk:false,magnet:true,rightSidebar:true,leftRail:true}, indicators:["BB20","KELTNER20","ATR14"] },
];

export function cloneWorkspace(workspace: ChartWorkspace, patch: Partial<ChartWorkspace> = {}): ChartWorkspace {
  return { ...workspace, ...patch, symbols:[...(patch.symbols ?? workspace.symbols)], indicators:[...(patch.indicators ?? workspace.indicators)], preferences:{...workspace.preferences,...patch.preferences} };
}

export function findWorkspace(workspaces: readonly ChartWorkspace[], id: string): ChartWorkspace | undefined {
  return workspaces.find(x => x.id === id);
}
