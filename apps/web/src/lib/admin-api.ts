import { z } from "zod";

const ComponentSchema = z.object({
  name: z.string(),
  required: z.boolean(),
  state: z.enum(["starting", "ready", "degraded", "stopped"]),
  started_at: z.string().nullable(),
  ready_at: z.string().nullable(),
  duration_ms: z.number().nullable(),
  detail: z.string(),
  checks: z.array(z.string()),
  error: z.string().nullable(),
});

const AdminRuntimeSchema = z.object({
  app: z.object({ name: z.string(), version: z.string(), environment: z.string() }),
  endpoints: z.object({ api_host: z.string(), api_port: z.number() }),
  dependencies: z.object({ postgres: z.boolean(), nats: z.boolean(), redis: z.boolean() }),
  platform: z.object({
    status: z.enum(["ready", "degraded"]),
    generation: z.number(),
    started_at: z.string().nullable(),
    component_count: z.number(),
    ready_count: z.number(),
    degraded_count: z.number(),
    components: z.array(ComponentSchema),
  }),
  security: z.object({
    secrets_exposed: z.boolean(),
    mutation_enabled: z.boolean(),
    authentication: z.string(),
  }),
});
export type AdminRuntime = z.infer<typeof AdminRuntimeSchema>;

const ConfigDefaultsSchema = z.object({
  intelligence_identity: z.object({
    name: z.string(),
    short_name: z.string(),
    domain: z.string(),
    description: z.string(),
  }),
  mutation_enabled: z.boolean(),
  secrets_exposed: z.boolean(),
});
export type ConfigDefaults = z.infer<typeof ConfigDefaultsSchema>;

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api";

export async function getAdminRuntime(): Promise<AdminRuntime> {
  const response = await fetch(`${apiBaseUrl}/admin/runtime`, { cache: "no-store" });
  if (!response.ok) throw new Error(`Admin runtime request failed: ${response.status}`);
  return AdminRuntimeSchema.parse(await response.json());
}

export async function getConfigDefaults(): Promise<ConfigDefaults> {
  const response = await fetch(`${apiBaseUrl}/config/defaults`, { cache: "no-store" });
  if (!response.ok) throw new Error(`Config defaults request failed: ${response.status}`);
  return ConfigDefaultsSchema.parse(await response.json());
}
