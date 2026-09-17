import { z } from "zod";

const HealthSchema = z.object({
  status: z.string(),
  service: z.string(),
  timestamp: z.string(),
});

export const MarketObservationSchema = z.object({
  id: z.string().uuid(),
  instrument_id: z.string().uuid(),
  observed_at: z.string(),
  bid: z.string().nullable(),
  ask: z.string().nullable(),
  last: z.string().nullable(),
  volume: z.string().nullable(),
  source: z.string(),
  source_event_id: z.string().nullable(),
  created_at: z.string(),
});

export type MarketObservation = z.infer<typeof MarketObservationSchema>;

const MarketObservationListSchema = z.array(MarketObservationSchema);

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api";

export async function getHealth() {
  const response = await fetch(`${apiBaseUrl}/health`, { cache: "no-store" });
  if (!response.ok) throw new Error(`API health request failed: ${response.status}`);
  return HealthSchema.parse(await response.json());
}

export async function getMarketObservations(symbol: string, venue: string, limit = 500) {
  const params = new URLSearchParams({ symbol, venue, limit: String(limit) });
  const response = await fetch(`${apiBaseUrl}/market/observations?${params}`, {
    cache: "no-store",
  });
  if (!response.ok) throw new Error(`Market observations request failed: ${response.status}`);
  return MarketObservationListSchema.parse(await response.json());
}
