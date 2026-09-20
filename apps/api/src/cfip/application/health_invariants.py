"""Application boundary for protected self-healing health evaluation."""

from cfip.domain.health_invariants import (
    HealthInvariantObservation,
    HealthInvariantReport,
    evaluate_invariants,
)


class HealthInvariantService:
    """Pure evaluator used before repair execution or self-development promotion."""

    @staticmethod
    def evaluate(observations: list[HealthInvariantObservation]) -> HealthInvariantReport:
        return evaluate_invariants(observations)
