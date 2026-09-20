"""Evidence gate for governed learning/model promotion.

This policy is intentionally conservative: health must be green, independent
evidence must exist, sample size must clear the configured minimum, and the
reported confidence must meet the promotion threshold.
"""
from pydantic import BaseModel, ConfigDict, Field


class LearningValidation(BaseModel):
    model_config = ConfigDict(extra="forbid")
    candidate_id: str = Field(min_length=1, max_length=128)
    validation_run_id: str = Field(min_length=1, max_length=128)
    independent_evidence_ids: tuple[str, ...] = ()
    outcome_count: int = Field(ge=0)
    confidence: float = Field(ge=0.0, le=1.0)
    health_green: bool
    contradictions: int = Field(default=0, ge=0)


class PromotionPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")
    minimum_outcomes: int = Field(default=100, ge=1)
    minimum_independent_evidence: int = Field(default=2, ge=1)
    minimum_confidence: float = Field(default=0.95, ge=0.0, le=1.0)
    max_contradictions: int = Field(default=0, ge=0)


def evaluate_learning_promotion(
    validation: LearningValidation,
    policy: PromotionPolicy = PromotionPolicy(),
) -> tuple[bool, tuple[str, ...]]:
    reasons: list[str] = []
    if not validation.health_green:
        reasons.append("health_gate_failed")
    if validation.outcome_count < policy.minimum_outcomes:
        reasons.append("insufficient_outcomes")
    if len(set(validation.independent_evidence_ids)) < policy.minimum_independent_evidence:
        reasons.append("insufficient_independent_evidence")
    if validation.confidence < policy.minimum_confidence:
        reasons.append("confidence_below_threshold")
    if validation.contradictions > policy.max_contradictions:
        reasons.append("contradictory_evidence")
    return not reasons, tuple(reasons)
