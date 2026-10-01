"""Tests for health and self-healing circuit-breaker invariants."""

from cfip.application.health import HealthService
from cfip.domain.health import CircuitBreaker, ComponentHealth, HealthPolicy


def _health() -> ComponentHealth:
    return ComponentHealth(
        component="analysis",
        status="unhealthy",
        observed_at=100,
        error_rate=0.2,
        invariant_failures=["analysis_safety"],
        evidence_ids=["incident-1"],
    )


def test_health_opens_circuit_at_failure_budget() -> None:
    policy = HealthPolicy(max_consecutive_failures=3)
    circuit = CircuitBreaker(component="analysis", consecutive_failures=2)
    result = HealthService.evaluate(_health(), circuit, policy)
    assert result.should_open_circuit
    assert result.next_state == "open"

    updated = HealthService.record_failure(circuit, observed_at=100, policy=policy)
    assert updated.state == "open"
    assert updated.consecutive_failures == 3


def test_health_success_resets_failure_budget() -> None:
    circuit = CircuitBreaker(
        component="analysis",
        state="open",
        consecutive_failures=4,
        opened_at=100,
    )
    updated = HealthService.record_success(circuit)
    assert updated.state == "closed"
    assert updated.consecutive_failures == 0


def test_open_circuit_respects_cooldown_before_probe() -> None:
    circuit = CircuitBreaker(
        component="analysis",
        state="open",
        opened_at=100,
        cooldown_seconds=300,
    )
    assert not HealthService.can_probe(circuit, now=399)
    assert HealthService.can_probe(circuit, now=400)
