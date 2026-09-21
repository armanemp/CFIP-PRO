import type { UTCTimestamp } from "lightweight-charts";

export type Timeframe = "1m" | "5m" | "15m" | "30m" | "1H" | "4H" | "1D" | "1W" | "1M";
export type ChartKind = "candles" | "bars" | "line" | "area" | "baseline";
export type Tool = "cursor" | "crosshair" | "trendline" | "ray" | "horizontal" | "vertical" | "rectangle" | "fib" | "measure" | "long" | "short";
export type InspectorTab = "market" | "watchlist" | "structure" | "intelligence" | "risk" | "objects";
export type Panel = "symbol" | "timeframe" | "chartType" | "indicators" | "settings" | "language" | null;
export type Locale = "en" | "fa" | "de" | "ar" | "tr" | "fr" | "es" | "pt" | "ru" | "zh" | "ja";

export interface Candle { time: UTCTimestamp; open: number; high: number; low: number; close: number; volume: number; bid?: number | null; ask?: number | null; }
export interface Point { time: UTCTimestamp; price: number }
/** Drawing coordinates are normalized chart timestamps, not arbitrary values. */
export type DrawingPoint = { time: UTCTimestamp; price: number };
export interface Drawing { tool: Exclude<Tool, "cursor" | "crosshair">; a: DrawingPoint; b: DrawingPoint; id: string; locked?: boolean; visible?: boolean }
export interface Zone { a: UTCTimestamp; b: UTCTimestamp; high: number; low: number; bullish: boolean }
export interface OrderBlock { time: UTCTimestamp; end: UTCTimestamp; high: number; low: number; bullish: boolean; strength: number }
export type StructureLabel = "HH" | "HL" | "LH" | "LL";
export interface StructurePoint { time: UTCTimestamp; price: number; high: boolean; label: StructureLabel }
export interface StructureEvent { time: UTCTimestamp; type: "BOS" | "CHoCH" | "MSS"; bullish: boolean; price: number }
export interface SymbolDefinition { symbol: string; base: string; quote: string; name: string; digits: number; category: "majors" | "minors" | "exotics" }
export interface StudyDefinition { id: string; name: string; group: "trend" | "momentum" | "volatility" | "volume" | "structure"; pane: "overlay" | "oscillator" }
export interface ChartPreferences { showGrid: boolean; showVolume: boolean; showSessions: boolean; showBidAsk: boolean; magnet: boolean; rightSidebar: boolean; leftRail: boolean; }
export const DEFAULT_PREFERENCES: ChartPreferences = { showGrid: true, showVolume: true, showSessions: false, showBidAsk: false, magnet: true, rightSidebar: true, leftRail: true };
export const timeframeSeconds: Record<Timeframe, number> = { "1m": 60, "5m": 300, "15m": 900, "30m": 1800, "1H": 3600, "4H": 14400, "1D": 86400, "1W": 604800, "1M": 2592000 };
