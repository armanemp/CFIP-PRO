import { z } from "zod";

const AdminRuntimeSchema = z.object({
  app: z.object({ name: z.string(), version: z.string(), environment: z.string() }),
  endpoints: z.object({ api_host: z.string(), api_port: z.number() }),
  dependencies: z.object({ postgres: z.boolean(), nats: z.boolean(), redis: z.boolean() }),
  security: z.object({ secrets_exposed: z.boolean(), mutation_enabled: z.boolean(), note: z.string() }),
});
export type AdminRuntime = z.infer<typeof AdminRuntimeSchema>;

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api";

export async function getAdminRuntime(): Promise<AdminRuntime> {
  const response = await fetch(`${apiBaseUrl}/admin/runtime`, { cache: "no-store" });
  if (!response.ok) throw new Error(`Admin runtime request failed: ${response.status}`);
  return AdminRuntimeSchema.parse(await response.json());
}
