import { ANALYSIS_POLICY } from "./analysis-policy";
import type { Candle, OrderBlock, StructureEvent, StructurePoint, Zone } from "./types";

export type AnalysisBias = "bullish" | "bearish" | "neutral";
export type AnalysisModuleId =
  | "trend"
  | "momentum"
  | "volatility"
  | "volume"
  | "structure"
  | "zones"
  | "liquidity"
  | "order_blocks";

export interface AnalysisEvidence {
  module: AnalysisModuleId;
  bias: AnalysisBias;
  score: number;
  confidence: number;
  summary: string;
  facts: string[];
}

export interface LiquidityPool {
  kind: "equal_high" | "equal_low";
  price: number;
  touches: number;
  start: Candle["time"];
  end: Candle["time"];
  swept: boolean;
}

export interface LiquiditySweep {
  time: Candle["time"];
  price: number;
  kind: "buy_side" | "sell_side";
  reclaimed: boolean;
  strength: number;
}

export interface Displacement {
  time: Candle["time"];
  bullish: boolean;
  bodyRatio: number;
  range: number;
  atrMultiple: number;
  strength: number;
}

export interface PremiumDiscountRange {
  high: number;
  low: number;
  equilibrium: number;
  current: number;
  zone: "premium" | "discount" | "equilibrium";
  sourceHigh: Candle["time"];
  sourceLow: Candle["time"];
}

export interface MTFStructureSummary {
  timeframe: string;
  bias: AnalysisBias;
  structure: "HH_HL" | "LH_LL" | "mixed" | "insufficient";
  lastEvent?: StructureEvent;
}

export interface ConfluenceGate {
  id: "htf_alignment" | "liquidity_or_fvg" | "zone_or_premium" | "displacement_or_structure";
  passed: boolean;
  detail: string;
}

export interface UnifiedAnalysis {
  bias: AnalysisBias;
  score: number;
  confidence: number;
  regime: "trending" | "ranging" | "volatile" | "mixed" | "insufficient";
  recommendation: "long" | "short" | "wait";
  modules: AnalysisEvidence[];
  liquidity: { pools: LiquidityPool[]; sweeps: LiquiditySweep[] };
  displacement: Displacement[];
  premiumDiscount: PremiumDiscountRange | null;
  mtf: MTFStructureSummary[];
  evidence: string[];
  confluence: {
    score: number;
    threshold: number;
    accepted: boolean;
    gates: ConfluenceGate[];
  };
}

export interface AnalysisContext {
  candles: Candle[];
  zones: Zone[];
  structurePoints: StructurePoint[];
  structureEvents: StructureEvent[];
  orderBlocks: OrderBlock[];
  liquidityPools: LiquidityPool[];
  liquiditySweeps: LiquiditySweep[];
  displacement: Displacement[];
  premiumDiscount: PremiumDiscountRange | null;
  mtf: MTFStructureSummary[];
  rsi: number | null;
  macdHistogram: number | null;
  atr: number | null;
}

function clamp(value: number, min = -1, max = 1) {
  return Math.max(min, Math.min(max, value));
}

export function aggregateAnalysis(ctx: AnalysisContext): UnifiedAnalysis {
  const modules: AnalysisEvidence[] = [];
  const last = ctx.candles.at(-1);
  const prev = ctx.candles.at(-2);
  if (!last) {
    return {
      bias: "neutral", score: 0, confidence: 0, regime: "insufficient", recommendation: "wait",
      modules: [], liquidity: { pools: ctx.liquidityPools, sweeps: ctx.liquiditySweeps },
      displacement: ctx.displacement, premiumDiscount: ctx.premiumDiscount, mtf: ctx.mtf, evidence: ["Insufficient market data."],
      confluence: { score: 0, threshold: ANALYSIS_POLICY.confluenceThreshold, accepted: false, gates: [] },
    };
  }

  const trendPoints = ctx.structurePoints.slice(-ANALYSIS_POLICY.maximumTrendPoints);
  const bullishStructure = trendPoints.filter(p => p.label === "HH" || p.label === "HL").length;
  const bearishStructure = trendPoints.filter(p => p.label === "LH" || p.label === "LL").length;
  const trendScore = clamp((bullishStructure - bearishStructure) / Math.max(1, trendPoints.length));
  modules.push({
    module: "trend", bias: trendScore > ANALYSIS_POLICY.neutralScoreThreshold ? "bullish" : trendScore < -ANALYSIS_POLICY.neutralScoreThreshold ? "bearish" : "neutral",
    score: trendScore, confidence: Math.min(1, trendPoints.length / 6),
    summary: trendScore > .2 ? "Structure is skewed bullish." : trendScore < -.2 ? "Structure is skewed bearish." : "Structure is mixed.",
    facts: trendPoints.slice(-3).map(p => p.label),
  });

  const momentumScore = ctx.rsi === null ? 0 : clamp((ctx.rsi - 50) / 25) * .65 + (ctx.macdHistogram === null ? 0 : Math.sign(ctx.macdHistogram) * .35);
  modules.push({
    module: "momentum", bias: momentumScore > ANALYSIS_POLICY.neutralScoreThreshold ? "bullish" : momentumScore < -ANALYSIS_POLICY.neutralScoreThreshold ? "bearish" : "neutral",
    score: momentumScore, confidence: ctx.rsi === null ? 0 : .8,
    summary: ctx.rsi === null ? "Momentum unavailable." : `RSI ${ctx.rsi.toFixed(1)} with MACD confirmation ${ctx.macdHistogram === null ? "unavailable" : ctx.macdHistogram >= 0 ? "positive" : "negative"}.`,
    facts: [ctx.rsi === null ? "RSI unavailable" : `RSI=${ctx.rsi.toFixed(1)}`],
  });

  const displacement = ctx.displacement.at(-1);
  const volScore = displacement ? (displacement.bullish ? 1 : -1) * Math.min(1, displacement.strength) : 0;
  modules.push({
    module: "volatility", bias: volScore > ANALYSIS_POLICY.neutralScoreThreshold ? "bullish" : volScore < -ANALYSIS_POLICY.neutralScoreThreshold ? "bearish" : "neutral",
    score: volScore, confidence: displacement ? .75 : 0,
    summary: displacement ? `Recent ${displacement.bullish ? "bullish" : "bearish"} displacement detected.` : "No qualifying displacement.",
    facts: displacement ? [`ATR multiple=${displacement.atrMultiple.toFixed(2)}`, `body/range=${(displacement.bodyRatio * 100).toFixed(0)}%`] : [],
  });

  const ob = ctx.orderBlocks.at(-1);
  const obScore = ob ? (ob.bullish ? 1 : -1) * ob.strength : 0;
  modules.push({
    module: "order_blocks", bias: obScore > ANALYSIS_POLICY.neutralScoreThreshold ? "bullish" : obScore < -ANALYSIS_POLICY.neutralScoreThreshold ? "bearish" : "neutral",
    score: obScore, confidence: ob ? .65 : 0,
    summary: ob ? `Latest order block is ${ob.bullish ? "bullish" : "bearish"}.` : "No qualifying order block.",
    facts: ob ? [`strength=${Math.round(ob.strength * 100)}%`] : [],
  });

  const zone = ctx.zones.at(-1);
  const zoneScore = zone ? (zone.bullish ? 1 : -1) : 0;
  modules.push({
    module: "zones", bias: zoneScore > 0 ? "bullish" : zoneScore < 0 ? "bearish" : "neutral",
    score: zoneScore * .5, confidence: zone ? .5 : 0,
    summary: zone ? `Latest FVG is ${zone.bullish ? "bullish" : "bearish"}.` : "No recent FVG.",
    facts: zone ? ["Fair value gap present"] : [],
  });

  const sweep = ctx.liquiditySweeps.at(-1);
  const liqScore = sweep ? (sweep.kind === "sell_side" ? 1 : -1) * sweep.strength : 0;
  modules.push({
    module: "liquidity", bias: liqScore > ANALYSIS_POLICY.neutralScoreThreshold ? "bullish" : liqScore < -ANALYSIS_POLICY.neutralScoreThreshold ? "bearish" : "neutral",
    score: liqScore, confidence: sweep ? .7 : 0,
    summary: sweep ? `${sweep.kind === "sell_side" ? "Sell-side" : "Buy-side"} liquidity sweep detected.` : "No recent liquidity sweep.",
    facts: sweep ? [sweep.reclaimed ? "Price reclaimed the liquidity level" : "Liquidity level was breached"] : [],
  });

  const vol = ctx.candles.length >= 2 ? Math.abs(last.close - prev!.close) / Math.max(last.high - last.low, Number.EPSILON) : 0;
  const volumeScore = last.volume > 0 ? (last.close >= last.open ? 1 : -1) * Math.min(1, vol) : 0;
  modules.push({
    module: "volume", bias: volumeScore > ANALYSIS_POLICY.neutralScoreThreshold ? "bullish" : volumeScore < -ANALYSIS_POLICY.neutralScoreThreshold ? "bearish" : "neutral",
    score: volumeScore, confidence: last.volume > 0 ? .45 : 0,
    summary: last.volume > 0 ? "Latest bar has directional volume participation." : "Volume unavailable.",
    facts: last.volume > 0 ? [`volume=${last.volume.toFixed(2)}`] : [],
  });

  const activeModules = modules.filter(m => m.confidence > 0);
  const weighted = activeModules.reduce((s, m) => s + m.score * m.confidence, 0);
  const weight = activeModules.reduce((s, m) => s + m.confidence, 0);
  const score = weight ? clamp(weighted / weight) : 0;
  const bias: AnalysisBias = score > .18 ? "bullish" : score < -.18 ? "bearish" : "neutral";
  const confidence = weight ? Math.min(1, Math.abs(weighted) / weight * .65 + Math.min(1, weight / 5) * .35) : 0;
  const range = ctx.atr && last.high - last.low > ctx.atr * 1.5 ? "volatile" : Math.abs(score) > .35 ? "trending" : "ranging";
  const evidence = modules.flatMap(m => m.facts.map(f => `${m.module}: ${f}`)).slice(-ANALYSIS_POLICY.maximumEvidenceItems);
  const alignedMtf = ctx.mtf.filter(m => m.bias === bias && m.structure !== "insufficient").length;
  const htfGate = alignedMtf >= 2;
  const liquidityOrFvg = ctx.liquiditySweeps.length > 0 || ctx.zones.length > 0;
  const zoneOrPremium = ctx.orderBlocks.length > 0 || ctx.premiumDiscount?.zone === (bias === "bullish" ? "discount" : bias === "bearish" ? "premium" : "equilibrium");
  const displacementOrStructure = ctx.displacement.length > 0 || ctx.structureEvents.length > 0;
  const gates: ConfluenceGate[] = [
    { id: "htf_alignment", passed: htfGate, detail: `${alignedMtf} aligned timeframes` },
    { id: "liquidity_or_fvg", passed: liquidityOrFvg, detail: liquidityOrFvg ? "Liquidity/FVG evidence present" : "No liquidity/FVG trigger" },
    { id: "zone_or_premium", passed: zoneOrPremium, detail: zoneOrPremium ? "Zone/premium-discount context present" : "No zone/premium context" },
    { id: "displacement_or_structure", passed: displacementOrStructure, detail: displacementOrStructure ? "Displacement/structure trigger present" : "No trigger confirmation" },
  ];
  const possible = modules.reduce((n, m) => n + (m.confidence > 0 ? 1 : 0), 0) + gates.length;
  const raw = modules.reduce((n, m) => n + Math.abs(m.score) * m.confidence, 0) + gates.filter(g => g.passed).length;
  const confluenceScore = possible ? Math.round(Math.max(0, Math.min(100, raw / possible * 100))) : 0;
  const accepted = confluenceScore >= ANALYSIS_POLICY.confluenceThreshold && gates.filter(g => g.passed).length >= ANALYSIS_POLICY.minimumPassedGates && bias !== "neutral";

  return {
    bias, score, confidence,
    regime: range,
    recommendation: accepted ? (bias === "bullish" ? "long" : "short") : "wait",
    modules,
    liquidity: { pools: ctx.liquidityPools, sweeps: ctx.liquiditySweeps },
    displacement: ctx.displacement,
    premiumDiscount: ctx.premiumDiscount,
    mtf: ctx.mtf,
    evidence,
    confluence: { score: confluenceScore, threshold: 84, accepted, gates },
  };
}
