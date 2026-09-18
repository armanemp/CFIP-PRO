"""Deterministic health evaluation and circuit-breaker transitions."""

from cfip.domain.health import (
    CircuitBreaker,
    ComponentHealth,
    HealthEvaluation,
    HealthPolicy,
)


class HealthService:
    @staticmethod
    def evaluate(
        health: ComponentHealth,
        circuit: CircuitBreaker,
        policy: HealthPolicy,
    ) -> HealthEvaluation:
        reasons: list[str] = []
        if health.status in {"unhealthy", "degraded"}:
            reasons.append(f"component_{health.status}")
        if health.error_rate is not None and health.error_rate > policy.max_error_rate:
            reasons.append("error_rate_exceeded")
        if health.latency_ms is not None and health.latency_ms > policy.max_latency_ms:
            reasons.append("latency_exceeded")
        if (
            health.freshness_seconds is not None
            and health.freshness_seconds > policy.max_freshness_seconds
        ):
            reasons.append("stale_data")
        if health.invariant_failures:
            reasons.append("safety_invariant_failure")

        should_open = (
            bool(reasons)
            and circuit.consecutive_failures + 1 >= policy.max_consecutive_failures
        )
        if should_open:
            return HealthEvaluation(
                status="unhealthy",
                reasons=reasons,
                should_open_circuit=True,
                next_state="open",
            )
        return HealthEvaluation(
            status="degraded" if reasons else "healthy",
            reasons=reasons,
            should_open_circuit=False,
            next_state="closed",
        )

    @staticmethod
    def record_failure(
        circuit: CircuitBreaker,
        *,
        observed_at: int,
        policy: HealthPolicy,
    ) -> CircuitBreaker:
        failures = circuit.consecutive_failures + 1
        if failures >= policy.max_consecutive_failures:
            return circuit.model_copy(
                update={
                    "state": "open",
                    "consecutive_failures": failures,
                    "opened_at": observed_at,
                }
            )
        return circuit.model_copy(update={"consecutive_failures": failures})

    @staticmethod
    def record_success(circuit: CircuitBreaker) -> CircuitBreaker:
        return circuit.model_copy(update={"state": "closed", "consecutive_failures": 0})

    @staticmethod
    def can_probe(circuit: CircuitBreaker, *, now: int) -> bool:
        if circuit.state == "closed":
            return True
        if circuit.state == "half_open":
            return True
        if circuit.opened_at is None:
            return False
        return now >= circuit.opened_at + circuit.cooldown_seconds
