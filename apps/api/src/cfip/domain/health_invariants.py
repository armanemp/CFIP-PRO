"""Deterministic safety invariants for self-healing and self-development.

These contracts describe protected platform conditions. They are deliberately
transport- and executor-neutral: evaluating an invariant must never mutate state.
"""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

InvariantId = Literal[
    "tests_green",
    "security_clean",
    "data_quality_healthy",
    "risk_gate_intact",
    "audit_chain_healthy",
    "authorization_intact",
    "market_data_fresh",
]


class HealthInvariantObservation(BaseModel):
    model_config = ConfigDict(extra="forbid")

    invariant_id: InvariantId
    passed: bool
    observed_at: int = Field(gt=0)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)
    detail: str = Field(min_length=1, max_length=1000)


class HealthInvariantReport(BaseModel):
    model_config = ConfigDict(extra="forbid")

    healthy: bool
    observations: list[HealthInvariantObservation] = Field(min_length=1, max_length=100)
    blocking_invariants: list[InvariantId] = Field(default_factory=list, max_length=100)


def evaluate_invariants(observations: list[HealthInvariantObservation]) -> HealthInvariantReport:
    """Evaluate a supplied snapshot without guessing missing safety evidence.

    Missing protected invariants are blocking. Duplicate IDs are rejected so a
    stale/ambiguous observation cannot accidentally mask a failed invariant.
    """
    if not observations:
        raise ValueError("invariant_snapshot_required")
    by_id: dict[str, HealthInvariantObservation] = {}
    for observation in observations:
        if observation.invariant_id in by_id:
            raise ValueError("duplicate_invariant_observation")
        by_id[observation.invariant_id] = observation

    required: tuple[InvariantId, ...] = (
        "tests_green",
        "security_clean",
        "data_quality_healthy",
        "risk_gate_intact",
        "audit_chain_healthy",
        "authorization_intact",
        "market_data_fresh",
    )
    blocking = [
        invariant_id
        for invariant_id in required
        if invariant_id not in by_id or not by_id[invariant_id].passed
    ]
    return HealthInvariantReport(
        healthy=not blocking,
        observations=observations,
        blocking_invariants=blocking,
    )
