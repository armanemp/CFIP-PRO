import pytest

from cfip.application.intelligence_runtime import IntelligenceRuntimeService
from cfip.domain.intelligence_contracts import AgentRequest, AgentSpec, IntelligenceExecutionContext


def request(*, evidence=True, dry_run=True):
    return AgentRequest(
        spec=AgentSpec(agent_id="analyst", version="1", model_id="model"),
        prompt="analyse",
        context=IntelligenceExecutionContext(
            request_id="r1",
            actor="test",
            purpose="analysis",
            evidence=(
                ({"evidence_id":"e1","source":"test","kind":"market","content_digest":"sha256:1234567890123456","observed_at":1},)
                if False else ()
            ),
            dry_run=dry_run,
            approval_required=True,
        ),
    )


def test_live_execution_is_blocked_without_approval():
    decision = IntelligenceRuntimeService.authorize(request(dry_run=False))
    assert not decision.allowed
    assert decision.reason == "evidence_required"


def test_dry_run_still_requires_evidence():
    decision = IntelligenceRuntimeService.authorize(request())
    assert not decision.allowed
    assert decision.reason == "evidence_required"
