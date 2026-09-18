import { z } from "zod";

const HealthSchema = z.object({ status: z.string(), service: z.string(), timestamp: z.string() });
export const MarketObservationSchema = z.object({ id: z.string().uuid(), instrument_id: z.string().uuid(), observed_at: z.string(), bid: z.string().nullable(), ask: z.string().nullable(), last: z.string().nullable(), volume: z.string().nullable(), source: z.string(), source_event_id: z.string().nullable(), created_at: z.string() });
export type MarketObservation = z.infer<typeof MarketObservationSchema>;
const MarketObservationListSchema = z.array(MarketObservationSchema);
const DemoBarSchema = z.object({ time: z.string(), open: z.number(), high: z.number(), low: z.number(), close: z.number(), volume: z.number() });
const DemoMarketSchema = z.object({ provider: z.string(), symbol: z.string(), ticker: z.string(), delay_note: z.string(), observed_at: z.string(), bid: z.number().nullable(), ask: z.number().nullable(), last: z.number().nullable(), bars: z.array(DemoBarSchema) });
export type DemoMarket = z.infer<typeof DemoMarketSchema>;
const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api";

export async function getHealth() { const response = await fetch(`${apiBaseUrl}/health`, { cache: "no-store" }); if (!response.ok) throw new Error(`API health request failed: ${response.status}`); return HealthSchema.parse(await response.json()); }
export async function getDemoEurusd(limit = 500): Promise<DemoMarket> { const response = await fetch(`${apiBaseUrl}/market/demo/eurusd?limit=${limit}`, { cache: "no-store" }); if (!response.ok) throw new Error(`Demo market provider failed: ${response.status}`); return DemoMarketSchema.parse(await response.json()); }
export async function getMarketObservations(symbol: string, venue: string, limit = 500) {
  const params = new URLSearchParams({ symbol, venue, limit: String(limit) });
  const response = await fetch(`${apiBaseUrl}/market/observations?${params}`, { cache: "no-store" });
  if (!response.ok) throw new Error(`Market observations request failed: ${response.status}`);
  const databaseRows = MarketObservationListSchema.parse(await response.json());
  if (databaseRows.length || symbol !== "EUR/USD") return databaseRows;
  const demo = await getDemoEurusd(Math.min(limit, 1000));
  const instrumentId = "00000000-0000-4000-8000-000000000001";
  const rows: MarketObservation[] = demo.bars.map((bar, index) => ({
    id: `00000000-0000-4000-8000-${String(index + 1).padStart(12, "0")}`,
    instrument_id: instrumentId,
    observed_at: new Date(bar.time.replace(" ", "T") + "Z").toISOString(),
    bid: null, ask: null, last: String(bar.close), volume: String(bar.volume), source: demo.provider, source_event_id: `demo-bar-${bar.time}`, created_at: new Date().toISOString(),
  }));
  if (demo.last !== null) rows.push({ id: "00000000-0000-4000-8000-999999999999", instrument_id: instrumentId, observed_at: demo.observed_at, bid: demo.bid === null ? null : String(demo.bid), ask: demo.ask === null ? null : String(demo.ask), last: String(demo.last), volume: null, source: demo.provider, source_event_id: `demo-quote-${demo.observed_at}`, created_at: new Date().toISOString() });
  return rows;
}


const AnalysisModuleSchema = z.object({
  module: z.string(),
  bias: z.enum(["bullish","bearish","neutral"]),
  score: z.number(),
  confidence: z.number(),
  summary: z.string(),
  facts: z.array(z.string()),
});
const AnalysisGateSchema = z.object({
  id: z.string(),
  passed: z.boolean(),
  detail: z.string(),
});
const RiskTargetSchema = z.object({
  available: z.boolean(),
  reason: z.string().nullable(),
  entry: z.number().nullable(),
  stop: z.number().nullable(),
  risk_distance: z.number().nullable(),
  tp1: z.number().nullable(),
  tp2: z.number().nullable(),
  tp3: z.number().nullable(),
  rr1: z.number().nullable(),
  rr2: z.number().nullable(),
  rr3: z.number().nullable(),
});
export const UnifiedAnalysisSchema = z.object({
  symbol: z.string(),
  timeframe: z.string(),
  as_of: z.string(),
  bias: z.enum(["bullish","bearish","neutral"]),
  score: z.number(),
  confidence: z.number(),
  regime: z.enum(["trending","ranging","volatile","mixed","insufficient"]),
  recommendation: z.enum(["long","short","wait"]),
  modules: z.array(AnalysisModuleSchema),
  confluence_score: z.number(),
  confluence_threshold: z.number(),
  confluence_accepted: z.boolean(),
  gates: z.array(AnalysisGateSchema),
  evidence: z.array(z.string()),
  risk_target: RiskTargetSchema,
  closed_bar_time: z.number().nullable(),
});
export type UnifiedAnalysisRead = z.infer<typeof UnifiedAnalysisSchema>;

export async function postUnifiedAnalysis(
  symbol: string,
  timeframe: string,
  candles: Array<{ time: number; open: number; high: number; low: number; close: number; volume: number }>,
): Promise<UnifiedAnalysisRead> {
  const response = await fetch(`${apiBaseUrl}/analysis/unified`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    cache: "no-store",
    body: JSON.stringify({ symbol, timeframe, candles, closed_bar_only: true }),
  });
  if (!response.ok) throw new Error(`Unified analysis request failed: ${response.status}`);
  return UnifiedAnalysisSchema.parse(await response.json());
}
