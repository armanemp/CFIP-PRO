"""Tests for the protected self-healing health snapshot."""

import pytest

from cfip.application.health_invariants import HealthInvariantService
from cfip.domain.health_invariants import HealthInvariantObservation


_IDS = (
    "tests_green",
    "security_clean",
    "data_quality_healthy",
    "risk_gate_intact",
    "audit_chain_healthy",
    "authorization_intact",
    "market_data_fresh",
)


def _snapshot(*, failed: str | None = None) -> list[HealthInvariantObservation]:
    return [
        HealthInvariantObservation(
            invariant_id=invariant_id,
            passed=invariant_id != failed,
            observed_at=1,
            evidence_ids=[f"evidence-{invariant_id}"],
            detail="healthy" if invariant_id != failed else "protected condition failed",
        )
        for invariant_id in _IDS
    ]


def test_complete_healthy_snapshot_allows_promotion() -> None:
    report = HealthInvariantService.evaluate(_snapshot())
    assert report.healthy
    assert report.blocking_invariants == []


def test_failed_risk_invariant_blocks_self_healing() -> None:
    report = HealthInvariantService.evaluate(_snapshot(failed="risk_gate_intact"))
    assert not report.healthy
    assert report.blocking_invariants == ["risk_gate_intact"]


def test_missing_protected_invariant_is_blocking() -> None:
    observations = _snapshot()[:-1]
    report = HealthInvariantService.evaluate(observations)
    assert not report.healthy
    assert report.blocking_invariants == ["market_data_fresh"]


def test_duplicate_invariant_observation_is_rejected() -> None:
    observations = _snapshot()
    observations.append(observations[0])
    with pytest.raises(ValueError, match="duplicate_invariant_observation"):
        HealthInvariantService.evaluate(observations)


def test_empty_snapshot_is_rejected() -> None:
    with pytest.raises(ValueError, match="invariant_snapshot_required"):
        HealthInvariantService.evaluate([])
