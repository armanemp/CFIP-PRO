import type { ChartKind, Locale, Timeframe, Tool } from "./types";

export const TIMEFRAMES: Timeframe[] = ["1m","5m","15m","30m","1H","4H","1D","1W","1M"];

export const INDICATORS = [
  "EMA20","EMA50","EMA200","SMA20","WMA20","VWAP","BB20",
  "RSI14","MACD","DMI14","STOCH14","DONCHIAN20","KELTNER20","ICHIMOKU",
] as const;

export const DRAWING_TOOLS: Tool[] = [
  "cursor","crosshair","trendline","ray","horizontal","vertical",
  "rectangle","fib","measure","long","short",
];

export const TOOL_GLYPHS: Record<Tool,string> = {
  cursor:"•", crosshair:"✛", trendline:"╱", ray:"↗", horizontal:"—",
  vertical:"│", rectangle:"□", fib:"F", measure:"↔", long:"↗", short:"↘",
};

export const CHART_KINDS: ChartKind[] = ["candles","bars","line","area","baseline"];

export const DEFAULT_WATCHLIST = [
  "EUR/USD","GBP/USD","USD/JPY","USD/CHF","AUD/USD","USD/CAD","NZD/USD","EUR/JPY",
] as const;

export const LOCALES: Locale[] = ["en","fa","de","ar","tr","fr","es","pt","ru","zh","ja"];
