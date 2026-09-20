"""Typed health evidence collection boundary for self-healing.

The collector does not invent health. Every protected invariant must be supplied
by an explicit signal with its own observation timestamp and evidence IDs.
"""

from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

from cfip.domain.health_invariants import HealthInvariantObservation, InvariantId


class HealthSignal(BaseModel):
    model_config = ConfigDict(extra="forbid")
    invariant_id: InvariantId
    passed: bool
    observed_at: int = Field(gt=0)
    evidence_ids: tuple[str, ...] = ()
    detail: str = Field(min_length=1, max_length=1000)


class HealthSignalSnapshot(BaseModel):
    model_config = ConfigDict(extra="forbid")
    signals: tuple[HealthSignal, ...] = ()
    collected_at: int = Field(gt=0)
    max_age_seconds: int = Field(default=300, gt=0)


def build_health_observations(
    snapshot: HealthSignalSnapshot,
    *,
    now: int,
) -> list[HealthInvariantObservation]:
    if now < snapshot.collected_at:
        raise ValueError("health_snapshot_from_future")
    by_id: dict[str, HealthSignal] = {}
    for signal in snapshot.signals:
        if signal.invariant_id in by_id:
            raise ValueError("duplicate_health_signal")
        if now - signal.observed_at > snapshot.max_age_seconds:
            raise ValueError(f"stale_health_signal:{signal.invariant_id}")
        if signal.observed_at > now:
            raise ValueError(f"future_health_signal:{signal.invariant_id}")
        by_id[signal.invariant_id] = signal

    return [
        HealthInvariantObservation(
            invariant_id=invariant_id,
            passed=signal.passed,
            observed_at=signal.observed_at,
            evidence_ids=list(signal.evidence_ids),
            detail=signal.detail,
        )
        for invariant_id, signal in by_id.items()
    ]
