import type { Timeframe, Candle } from "./types";
import { atr, fvg, liquidityAnalysis, displacementAnalysis, marketStructure, mtfStructure, orderBlocks, premiumDiscount, rsi, macd } from "./chart-math";
import { aggregateAnalysis, type UnifiedAnalysis } from "./analysis-contracts";

export interface AnalysisSnapshot {
  analysis: UnifiedAnalysis;
  rsi: number | null;
  macdHistogram: number | null;
  atr: number | null;
}

export function computeAnalysisSnapshot(candles: Candle[], timeframe: Timeframe): AnalysisSnapshot {
  const source = candles.length > 1 ? candles.slice(0, -1) : candles;
  const zones = fvg(source);
  const structure = marketStructure(source);
  const blocks = orderBlocks(source);
  const liquidity = liquidityAnalysis(source);
  const displacement = displacementAnalysis(source);
  const premiumDiscountValue = premiumDiscount(source);
  const mtf = mtfStructure(source, timeframe);
  const rsiValue = rsi(source, 14).at(-1)?.value ?? null;
  const macdHistogram = macd(source).histogram.at(-1)?.value ?? null;
  const atrValue = atr(source, 14).at(-1)?.value ?? null;

  return {
    rsi: rsiValue,
    macdHistogram,
    atr: atrValue,
    analysis: aggregateAnalysis({
      candles: source,
      zones,
      structurePoints: structure.points,
      structureEvents: structure.events,
      orderBlocks: blocks,
      liquidityPools: liquidity.pools,
      liquiditySweeps: liquidity.sweeps,
      displacement,
      premiumDiscount: premiumDiscountValue,
      mtf,
      rsi: rsiValue,
      macdHistogram,
      atr: atrValue,
    }),
  };
}
