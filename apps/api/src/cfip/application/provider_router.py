"""Deterministic provider selection with health-aware fallback ordering."""
from __future__ import annotations
from dataclasses import dataclass
from cfip.domain.config_contracts import ProviderConfig
from cfip.domain.provider_contracts import PROVIDER_CATALOG, ProviderDescriptor

@dataclass(frozen=True)
class ProviderRouter:
    configs: tuple[ProviderConfig, ...] = ()

    def select(self, kind: str, capability: str) -> tuple[ProviderDescriptor, ...]:
        configured = {item.provider_id: item for item in self.configs if item.enabled}
        catalog = [item for item in PROVIDER_CATALOG if item.kind == kind and capability in item.capabilities]
        catalog.sort(key=lambda item: configured.get(item.id, ProviderConfig(provider_id=item.id)).priority)
        return tuple(catalog)
