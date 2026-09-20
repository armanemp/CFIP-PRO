from cfip.domain.learning_promotion import LearningValidation, PromotionPolicy, evaluate_learning_promotion


def test_learning_promotion_requires_independent_evidence_and_health() -> None:
    validation = LearningValidation(
        candidate_id="candidate-1",
        validation_run_id="run-1",
        independent_evidence_ids=("e1", "e2"),
        outcome_count=120,
        confidence=0.97,
        health_green=True,
    )
    approved, reasons = evaluate_learning_promotion(validation)
    assert approved
    assert reasons == ()


def test_learning_promotion_fails_closed_on_weak_evidence() -> None:
    validation = LearningValidation(
        candidate_id="candidate-1",
        validation_run_id="run-2",
        independent_evidence_ids=("e1", "e1"),
        outcome_count=40,
        confidence=0.80,
        health_green=False,
        contradictions=1,
    )
    approved, reasons = evaluate_learning_promotion(validation)
    assert not approved
    assert set(reasons) == {
        "health_gate_failed",
        "insufficient_outcomes",
        "insufficient_independent_evidence",
        "confidence_below_threshold",
        "contradictory_evidence",
    }
