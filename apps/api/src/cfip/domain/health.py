"""Governed health, incident and self-healing circuit-breaker contracts."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

HealthStatus = Literal["healthy", "degraded", "unhealthy", "unknown"]
IncidentState = Literal["open", "mitigating", "resolved", "escalated", "suppressed"]
CircuitState = Literal["closed", "open", "half_open"]


class ComponentHealth(BaseModel):
    model_config = ConfigDict(extra="forbid")

    component: str = Field(min_length=1, max_length=128)
    status: HealthStatus
    observed_at: int = Field(gt=0)
    latency_ms: float | None = Field(default=None, ge=0)
    error_rate: float | None = Field(default=None, ge=0, le=1)
    freshness_seconds: int | None = Field(default=None, ge=0)
    invariant_failures: list[str] = Field(default_factory=list, max_length=100)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)


class Incident(BaseModel):
    model_config = ConfigDict(extra="forbid")

    incident_id: str = Field(min_length=1, max_length=128)
    component: str = Field(min_length=1, max_length=128)
    state: IncidentState
    severity: Literal["low", "medium", "high", "critical"]
    opened_at: int = Field(gt=0)
    last_observed_at: int = Field(gt=0)
    failure_count: int = Field(ge=0)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)

    @model_validator(mode="after")
    def validate_times(self) -> "Incident":
        if self.last_observed_at < self.opened_at:
            raise ValueError("incident_observation_before_open")
        return self


class CircuitBreaker(BaseModel):
    model_config = ConfigDict(extra="forbid")

    component: str = Field(min_length=1, max_length=128)
    state: CircuitState = "closed"
    consecutive_failures: int = Field(default=0, ge=0)
    opened_at: int | None = Field(default=None, gt=0)
    cooldown_seconds: int = Field(default=300, ge=1, le=86400)


class HealthPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")

    max_error_rate: float = Field(default=0.05, gt=0, le=1)
    max_latency_ms: float = Field(default=5000, gt=0)
    max_freshness_seconds: int = Field(default=300, ge=1)
    max_consecutive_failures: int = Field(default=3, ge=1, le=100)
    cooldown_seconds: int = Field(default=300, ge=1, le=86400)


class HealthEvaluation(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: HealthStatus
    reasons: list[str] = Field(default_factory=list, max_length=50)
    should_open_circuit: bool
    next_state: CircuitState
