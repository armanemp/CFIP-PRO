export type TerminalCapabilityKind = "chart" | "analysis" | "market" | "trading" | "research" | "intelligence" | "workspace" | "system";
export type TerminalCapabilityStatus = "native" | "adapter" | "contract" | "planned";

export interface TerminalCapability {
  id: string;
  labelKey: string;
  kind: TerminalCapabilityKind;
  status: TerminalCapabilityStatus;
  shortcut?: string;
  requires?: readonly string[];
}

export const TERMINAL_CAPABILITIES: readonly TerminalCapability[] = [
  {id:"chart.crosshair",labelKey:"crosshair",kind:"chart",status:"native"},
  {id:"chart.drawings",labelKey:"drawings",kind:"chart",status:"native"},
  {id:"chart.indicators",labelKey:"indicators",kind:"chart",status:"native"},
  {id:"chart.multi-pane",labelKey:"multiPane",kind:"chart",status:"native"},
  {id:"chart.replay",labelKey:"replay",kind:"chart",status:"native"},
  {id:"chart.templates",labelKey:"templates",kind:"workspace",status:"contract"},
  {id:"chart.compare",labelKey:"compare",kind:"chart",status:"contract"},
  {id:"chart.export",labelKey:"export",kind:"chart",status:"contract"},
  {id:"chart.command-search",labelKey:"commandPalette",kind:"chart",status:"contract",shortcut:"Ctrl/Cmd+K"},
  {id:"chart.object-manager",labelKey:"objects",kind:"chart",status:"contract"},
  {id:"chart.chart-types",labelKey:"chartType",kind:"chart",status:"native"},
  {id:"chart.undo-redo",labelKey:"undoRedo",kind:"chart",status:"native"},
  {id:"market.watchlist",labelKey:"watchlist",kind:"market",status:"native"},
  {id:"market.screener",labelKey:"screener",kind:"market",status:"contract"},
  {id:"market.depth",labelKey:"depth",kind:"market",status:"contract"},
  {id:"market.time-sales",labelKey:"timeSales",kind:"market",status:"contract"},
  {id:"market.bid-ask",labelKey:"bidAsk",kind:"market",status:"contract"},
  {id:"market.spread",labelKey:"spread",kind:"market",status:"contract"},
  {id:"market.sessions",labelKey:"sessions",kind:"market",status:"contract"},
  {id:"trading.risk",labelKey:"risk",kind:"trading",status:"native"},
  {id:"trading.paper",labelKey:"paperTrading",kind:"trading",status:"contract"},
  {id:"trading.orders",labelKey:"orders",kind:"trading",status:"contract"},
  {id:"trading.positions",labelKey:"positions",kind:"trading",status:"contract"},
  {id:"trading.portfolio",labelKey:"portfolio",kind:"trading",status:"contract"},
  {id:"trading.execution",labelKey:"execution",kind:"trading",status:"contract"},
  {id:"trading.journal",labelKey:"journal",kind:"trading",status:"contract"},
  {id:"analysis.structure",labelKey:"structure",kind:"analysis",status:"native"},
  {id:"analysis.fvg",labelKey:"fvg",kind:"analysis",status:"native"},
  {id:"analysis.order-blocks",labelKey:"orderBlocks",kind:"analysis",status:"native"},
  {id:"analysis.mtf",labelKey:"multiTimeframe",kind:"analysis",status:"contract"},
  {id:"analysis.backtest",labelKey:"backtest",kind:"analysis",status:"contract"},
  {id:"analysis.confluence",labelKey:"confluence",kind:"analysis",status:"contract"},
  {id:"analysis.liquidity",labelKey:"liquidity",kind:"analysis",status:"native"},
  {id:"intelligence.core",labelKey:"intelligence",kind:"intelligence",status:"contract"},
  {id:"intelligence.research",labelKey:"research",kind:"research",status:"contract"},
  {id:"intelligence.proposals",labelKey:"proposals",kind:"intelligence",status:"contract"},
  {id:"intelligence.self-healing",labelKey:"selfHealing",kind:"intelligence",status:"contract"},
  {id:"workspace.saved",labelKey:"workspace",kind:"workspace",status:"native"},
  {id:"workspace.layouts",labelKey:"layouts",kind:"workspace",status:"contract"},
  {id:"system.command-palette",labelKey:"commandPalette",kind:"system",status:"contract",shortcut:"Ctrl/Cmd+K"},
  {id:"system.notifications",labelKey:"notification",kind:"system",status:"contract"},
];

export function capabilitiesByKind(kind: TerminalCapabilityKind): TerminalCapability[] {
  return TERMINAL_CAPABILITIES.filter(capability => capability.kind === kind);
}

export function capability(id: string): TerminalCapability | undefined {
  return TERMINAL_CAPABILITIES.find(item => item.id === id);
}
