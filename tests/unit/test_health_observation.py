import pytest

from cfip.domain.health_observation import (
    HealthSignal,
    HealthSignalSnapshot,
    build_health_observations,
)


def test_health_snapshot_converts_explicit_signals() -> None:
    snapshot = HealthSignalSnapshot(
        collected_at=100,
        max_age_seconds=10,
        signals=(
            HealthSignal(
                invariant_id="risk_gate_intact",
                passed=True,
                observed_at=95,
                evidence_ids=("risk-check-1",),
                detail="risk gate verified",
            ),
        ),
    )
    observations = build_health_observations(snapshot, now=100)
    assert observations[0].invariant_id == "risk_gate_intact"
    assert observations[0].passed is True


def test_stale_health_signal_is_rejected() -> None:
    snapshot = HealthSignalSnapshot(
        collected_at=100,
        max_age_seconds=10,
        signals=(
            HealthSignal(
                invariant_id="tests_green",
                passed=True,
                observed_at=89,
                detail="old",
            ),
        ),
    )
    with pytest.raises(ValueError, match="stale_health_signal"):
        build_health_observations(snapshot, now=100)


def test_duplicate_health_signal_is_rejected() -> None:
    signal = HealthSignal(
        invariant_id="security_clean",
        passed=True,
        observed_at=100,
        detail="ok",
    )
    snapshot = HealthSignalSnapshot(collected_at=100, signals=(signal, signal))
    with pytest.raises(ValueError, match="duplicate_health_signal"):
        build_health_observations(snapshot, now=100)
