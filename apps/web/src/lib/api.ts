import { z } from "zod";

const HealthSchema = z.object({
  status: z.string(),
  service: z.string(),
  timestamp: z.string(),
});

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://127.0.0.1:8000/api";

export async function getHealth() {
  const response = await fetch(`${apiBaseUrl}/health`, { cache: "no-store" });
  if (!response.ok) throw new Error(`API health request failed: ${response.status}`);
  return HealthSchema.parse(await response.json());
}
