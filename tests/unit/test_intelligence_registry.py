from cfip.application.intelligence_registry import InMemoryIntelligenceRecordRepository
from cfip.domain.intelligence_governance import EvidenceRef, ImprovementProposal, LearningOutcome, ValidationPlan, ValidationResult


def _proposal() -> ImprovementProposal:
    return ImprovementProposal(
        id="proposal-1",
        title="Improve data freshness diagnostics",
        rationale="Detect stale provider observations before analysis.",
        risk="low",
        evidence=[EvidenceRef(evidence_id="e1", source="runtime", content_hash="a" * 16, observed_at=1)],
        validation=ValidationPlan(checks=["freshness"], acceptance_criteria=["freshness_ok"], rollback_conditions=["regression"]),
    )


def test_registry_keeps_proposal_validation_and_learning_separate() -> None:
    repository = InMemoryIntelligenceRecordRepository()
    proposal = _proposal()
    repository.save_proposal(proposal)
    repository.save_validation(ValidationResult(proposal_id=proposal.id, passed=True, checks={"freshness": True}, evidence_ids=["e1"], summary="passed"))
    repository.save_learning(LearningOutcome(proposal_id=proposal.id, outcome="improved", observed_at=2, lesson="freshness gate reduced stale analysis"))

    assert repository.proposals() == (proposal,)
    assert repository.validations()[0].proposal_id == proposal.id
    assert repository.learnings()[0].outcome == "improved"
