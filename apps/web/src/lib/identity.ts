import { z } from "zod";

const IdentitySchema = z.object({
  name: z.string().min(2),
  short_name: z.string().min(2),
  domain: z.string(),
  description: z.string(),
});
export type PlatformIdentity = z.infer<typeof IdentitySchema>;

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "/api";

export async function getPlatformIdentity(): Promise<PlatformIdentity> {
  const response = await fetch(`${apiBaseUrl}/admin/identity`, { cache: "no-store" });
  if (!response.ok) throw new Error(`Identity request failed: ${response.status}`);
  return IdentitySchema.parse(await response.json());
}

export async function updatePlatformIdentity(name: string, adminToken: string): Promise<PlatformIdentity> {
  const response = await fetch(`${apiBaseUrl}/admin/identity`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", "X-Admin-Control-Token": adminToken },
    body: JSON.stringify({ name }),
  });
  if (!response.ok) throw new Error((await response.text()) || `Identity update failed: ${response.status}`);
  return IdentitySchema.parse(await response.json());
}
