"""Deterministic provider selection policy.

Selection is capability- and health-aware and preserves configured priority. It never
silently falls back to an incompatible provider.
"""
from pydantic import BaseModel, ConfigDict, Field
from cfip.domain.provider_contracts import ProviderDescriptor
from cfip.domain.provider_health import ProviderHealth

class ProviderSelectionPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_ids: tuple[str, ...] = ()
    required_capabilities: tuple[str, ...] = ()
    allow_degraded: bool = False

def select_providers(
    catalog: tuple[ProviderDescriptor, ...],
    health: tuple[ProviderHealth, ...],
    policy: ProviderSelectionPolicy,
) -> tuple[ProviderDescriptor, ...]:
    catalog_by_id = {item.id: item for item in catalog}
    health_by_id = {item.provider_id: item for item in health}
    selected: list[ProviderDescriptor] = []
    for provider_id in policy.provider_ids:
        provider = catalog_by_id.get(provider_id)
        state = health_by_id.get(provider_id)
        if provider is None or state is None or state.state in {"down", "unknown"}:
            continue
        if state.state == "degraded" and not policy.allow_degraded:
            continue
        if any(cap not in provider.capabilities for cap in policy.required_capabilities):
            continue
        selected.append(provider)
    return tuple(selected)
