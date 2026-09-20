import pytest
from pydantic import ValidationError

from cfip.application.self_improvement import evaluate_promotion_gate
from cfip.domain.self_improvement import ImprovementProposal


def proposal(**overrides: object) -> ImprovementProposal:
    values: dict[str, object] = dict(
        id="p1",
        kind="bug-fix",
        title="Fix",
        rationale="Evidence-backed",
        evidence_ids=("e1",),
        affected_paths=("apps/",),
        validation_plan=("pytest",),
        rollback_plan=("revert",),
        risk="low",
    )
    values.update(overrides)
    return ImprovementProposal(**values)


def test_gate_accepts_evidenced_low_risk_proposal() -> None:
    assert evaluate_promotion_gate(proposal()).accepted


def test_model_rejects_missing_governance_evidence() -> None:
    with pytest.raises(ValidationError):
        proposal(evidence_ids=())
    with pytest.raises(ValidationError):
        proposal(validation_plan=())
    with pytest.raises(ValidationError):
        proposal(rollback_plan=())


def test_high_risk_requires_human_approval() -> None:
    with pytest.raises(ValidationError):
        proposal(risk="high", requires_approval=False)
    result = evaluate_promotion_gate(proposal(risk="high"))
    assert not result.accepted
    assert "human_approval_required" in result.reasons


def test_proposal_rejects_duplicate_evidence_and_paths() -> None:
    with pytest.raises(ValidationError):
        proposal(evidence_ids=("e1", "e1"))
    with pytest.raises(ValidationError):
        proposal(affected_paths=("apps/", "apps/"))


def test_fingerprint_is_deterministic() -> None:
    first = proposal()
    second = proposal()
    assert first.fingerprint() == second.fingerprint()
    assert len(first.fingerprint()) == 64
