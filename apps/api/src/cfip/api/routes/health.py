"""Health and protected-invariant evaluation endpoints."""

from datetime import UTC, datetime

from fastapi import APIRouter
from pydantic import BaseModel, ConfigDict

from cfip.application.health import HealthService
from cfip.application.health_invariants import HealthInvariantService
from cfip.domain.health import CircuitBreaker, ComponentHealth, HealthEvaluation, HealthPolicy
from cfip.domain.health_invariants import HealthInvariantObservation, HealthInvariantReport

router = APIRouter()


class HealthResponse(BaseModel):
    status: str
    service: str
    timestamp: datetime


class HealthEvaluationRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    health: ComponentHealth
    circuit: CircuitBreaker
    policy: HealthPolicy = HealthPolicy()


class HealthInvariantRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    observations: list[HealthInvariantObservation]


@router.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    return HealthResponse(status="ok", service="api", timestamp=datetime.now(UTC))


@router.post("/health/evaluate", response_model=HealthEvaluation)
async def evaluate_health(request: HealthEvaluationRequest) -> HealthEvaluation:
    """Evaluate one component without mutating the circuit state."""
    return HealthService.evaluate(request.health, request.circuit, request.policy)


@router.post("/health/invariants", response_model=HealthInvariantReport)
async def evaluate_invariants(request: HealthInvariantRequest) -> HealthInvariantReport:
    """Evaluate all protected invariants; missing evidence remains blocking."""
    return HealthInvariantService.evaluate(request.observations)
