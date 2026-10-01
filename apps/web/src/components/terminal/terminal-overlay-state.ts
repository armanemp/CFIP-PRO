import type { Candle, ChartKind, Drawing, Timeframe } from "./types";
import { fvg, marketStructure, orderBlocks, pivots, sessionRange, supportResistance } from "./chart-math";
import { computeAnalysisSnapshot, type AnalysisSnapshot } from "./analysis-engine";

export interface TerminalOverlayState {
  zones: ReturnType<typeof fvg>;
  pivotPoints: ReturnType<typeof pivots>;
  levels: ReturnType<typeof supportResistance>;
  session: ReturnType<typeof sessionRange>;
  structure: ReturnType<typeof marketStructure>;
  blocks: ReturnType<typeof orderBlocks>;
  analysisSnapshot: AnalysisSnapshot;
  analysisCandles: Candle[];
}

export function computeTerminalOverlayState(candles: Candle[], timeframe: Timeframe): TerminalOverlayState {
  const analysisCandles = candles.length > 1 ? candles.slice(0, -1) : candles;
  return {
    zones: fvg(candles),
    pivotPoints: pivots(candles),
    levels: supportResistance(candles),
    session: sessionRange(candles),
    structure: marketStructure(candles),
    blocks: orderBlocks(candles),
    analysisSnapshot: computeAnalysisSnapshot(candles, timeframe),
    analysisCandles,
  };
}

export function drawingToolSupportsOverlay(tool: Drawing["tool"]): boolean {
  return ["trendline", "ray", "horizontal", "vertical", "rectangle", "fib", "measure", "long", "short"].includes(tool);
}

export function isPriceChartKind(kind: ChartKind): boolean {
  return kind === "candles" || kind === "bars" || kind === "line" || kind === "area" || kind === "baseline";
}
