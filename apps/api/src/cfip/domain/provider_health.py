from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

HealthState = Literal["unknown", "healthy", "degraded", "stale", "offline"]

class ProviderHealth(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str = Field(min_length=1, max_length=80)
    state: HealthState = "unknown"
    observed_at: int | None = Field(default=None, gt=0)
    latency_ms: float | None = Field(default=None, ge=0)
    error_rate: float = Field(default=0, ge=0, le=1)
    consecutive_failures: int = Field(default=0, ge=0)
    last_error: str | None = Field(default=None, max_length=1000)


def classify_provider_health(latency_ms: float | None, error_rate: float, consecutive_failures: int) -> HealthState:
    if consecutive_failures >= 5:
        return "offline"
    if error_rate >= 0.25:
        return "degraded"
    if latency_ms is not None and latency_ms > 3000:
        return "stale"
    if latency_ms is not None and error_rate < 0.05:
        return "healthy"
    return "unknown"
