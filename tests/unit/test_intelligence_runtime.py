from cfip.application.intelligence_runtime import IntelligenceRuntimeService
from cfip.domain.intelligence_contracts import (
    AgentRequest,
    AgentSpec,
    IntelligenceExecutionContext,
    IntelligenceEvidence,
)


def request(*, evidence=True, dry_run=True):
    return AgentRequest(
        spec=AgentSpec(agent_id="analyst", version="1", model_id="model"),
        prompt="analyse",
        context=IntelligenceExecutionContext(
            request_id="r1",
            actor="test",
            purpose="analysis",
            evidence=(
                IntelligenceEvidence(
                    evidence_id="e1", source="test", kind="market",
                    content_digest="sha256:1234567890123456", observed_at=1,
                ),
            ) if evidence else (),
            dry_run=dry_run,
            approval_required=True,
        ),
    )


def test_live_execution_is_blocked_without_evidence():
    decision = IntelligenceRuntimeService.authorize(request(evidence=False, dry_run=False))
    assert not decision.allowed
    assert decision.reason == "evidence_required"


def test_live_execution_requires_approval():
    decision = IntelligenceRuntimeService.authorize(request(evidence=True, dry_run=False))
    assert not decision.allowed
    assert decision.reason == "approval_required_for_live_execution"


def test_dry_run_with_evidence_is_allowed():
    decision = IntelligenceRuntimeService.authorize(request(evidence=True, dry_run=True))
    assert decision.allowed
    assert decision.reason == "allowed"
