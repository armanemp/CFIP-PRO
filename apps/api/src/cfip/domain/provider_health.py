"""Provider health and deterministic failover contracts."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

HealthState = Literal["healthy","degraded","stale","down","unknown"]

class ProviderHealth(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str = Field(min_length=1, max_length=80)
    state: HealthState = "unknown"
    latency_ms: float | None = Field(default=None, ge=0)
    observed_at: int | None = Field(default=None, gt=0)
    error_rate: float | None = Field(default=None, ge=0, le=1)
    capabilities: tuple[str, ...] = ()

class FailoverPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_ids: tuple[str, ...] = ()
    max_staleness_seconds: int = Field(default=30, ge=0)
    require_capability: str | None = None
    minimum_healthy_providers: int = Field(default=1, ge=0)

def eligible_providers(health: tuple[ProviderHealth, ...], policy: FailoverPolicy) -> tuple[str, ...]:
    by_id = {item.provider_id: item for item in health}
    result: list[str] = []
    for provider_id in policy.provider_ids:
        item = by_id.get(provider_id)
        if item is None or item.state in {"down", "unknown"}:
            continue
        if policy.require_capability and policy.require_capability not in item.capabilities:
            continue
        result.append(provider_id)
    return tuple(result)
