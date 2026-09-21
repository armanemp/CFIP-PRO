from cfip.application.self_improvement import evaluate_promotion_gate
from cfip.domain.self_improvement import ImprovementProposal

def proposal(**overrides):
    values = dict(id="p1",kind="bug-fix",title="Fix",rationale="Evidence-backed",evidence_ids=("e1",),affected_paths=("apps/",),validation_plan=("pytest",),rollback_plan=("revert",),risk="low")
    values.update(overrides)
    return ImprovementProposal(**values)

def test_gate_accepts_evidenced_low_risk_proposal() -> None:
    assert evaluate_promotion_gate(proposal()).accepted

def test_gate_rejects_missing_evidence_validation_and_rollback() -> None:
    result = evaluate_promotion_gate(proposal(evidence_ids=(), validation_plan=(), rollback_plan=()))
    assert not result.accepted
    assert set(result.reasons) == {"missing_evidence","missing_validation_plan","missing_rollback_plan"}

def test_high_risk_requires_human_approval() -> None:
    result = evaluate_promotion_gate(proposal(risk="high"))
    assert not result.accepted
    assert "human_approval_required" in result.reasons
