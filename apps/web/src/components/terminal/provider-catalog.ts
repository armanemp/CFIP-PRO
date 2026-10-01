import type { ProviderDescriptor } from "@/lib/api";

export interface ProviderFilter { kind?: ProviderDescriptor["kind"]; status?: ProviderDescriptor["status"]; }

export function filterProviders(providers: readonly ProviderDescriptor[], filter: ProviderFilter = {}): ProviderDescriptor[] {
  return providers.filter(provider =>
    (!filter.kind || provider.kind === filter.kind) &&
    (!filter.status || provider.status === filter.status)
  );
}

export function providerCapabilitySet(provider: ProviderDescriptor): Set<string> {
  return new Set(provider.capabilities);
}
