import pytest
from pydantic import ValidationError
from cfip.domain.self_development import ImprovementProposal, ImprovementTransition


def base(stage="observed"):
    return dict(proposal_id="imp-1", component="analysis", change_type="workflow", stage=stage,
        rationale="Improve validation", evidence=[{"evidence_id":"ev-1","digest":"sha256:"+"a"*16,"source":"test","observed_at":1}],
        test_plan=["unit"], rollback_plan="restore previous revision", risk_level="low")


def test_approval_requires_explicit_actor():
    with pytest.raises(ValidationError, match="promotion_requires_explicit_approval"):
        ImprovementProposal(**base("approved"))


def test_applied_requires_revision_and_approval():
    item = ImprovementProposal(**base("applied"), approved_by="admin", applied_revision="abc123")
    assert item.applied_revision == "abc123"


def test_applied_transition_must_follow_approval():
    with pytest.raises(ValidationError, match="applied_requires_approved_stage"):
        ImprovementTransition(proposal_id="imp-1", from_stage="validated", to_stage="applied", actor="admin",
            occurred_at=1, evidence_ids=["ev-1"], revision="abc123", reason="apply")


def test_rollback_requires_revision():
    with pytest.raises(ValidationError, match="rollback_requires_revision"):
        ImprovementTransition(proposal_id="imp-1", from_stage="applied", to_stage="rolled_back", actor="admin",
            occurred_at=1, evidence_ids=["ev-1"], reason="rollback")
