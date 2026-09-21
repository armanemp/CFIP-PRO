import type { ChartKind, Locale, Timeframe, Tool } from "./types";
import { INDICATOR_IDS } from "./indicator-registry";

export const TIMEFRAMES: Timeframe[] = ["1m","5m","15m","30m","1H","4H","1D","1W","1M"];
export const INDICATORS = INDICATOR_IDS;
export const DRAWING_TOOLS: Tool[] = ["cursor","crosshair","trendline","ray","horizontal","vertical","rectangle","fib","measure","long","short"];
export const TOOL_GLYPHS: Record<Tool,string> = { cursor:"•", crosshair:"✛", trendline:"╱", ray:"↗", horizontal:"—", vertical:"│", rectangle:"□", fib:"F", measure:"↔", long:"↗", short:"↘" };
export const CHART_KINDS: ChartKind[] = ["candles","bars","line","area","baseline"];
export const DEFAULT_WATCHLIST = ["EUR/USD","GBP/USD","USD/JPY","USD/CHF","AUD/USD","USD/CAD","NZD/USD","EUR/JPY"] as const;
export const LOCALES: Locale[] = ["en","fa","de","ar","tr","fr","es","pt","ru","zh","ja"];

export interface TerminalDataDefaults { symbol: string; venue: string; initialObservationLimit: number; refreshMs: number; }
export const TERMINAL_DATA_DEFAULTS: TerminalDataDefaults = { symbol: DEFAULT_WATCHLIST[0], venue: "reference", initialObservationLimit: 5000, refreshMs: 2000 };

export const TERMINAL_CHART_LAYOUT = Object.freeze({
  fontSize: 13,
  fontFamily: "Inter,Segoe UI,Arial,sans-serif",
  priceScaleMinWidth: 86,
  rightOffset: 10,
  barSpacing: 9,
  minBarSpacing: 2,
  maxBarSpacing: 30,
  oscillatorPaneHeight: 170,
  volumePaneHeight: 110,
});
