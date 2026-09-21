from cfip.application.intelligence_runtime import IntelligenceRuntimeService
from cfip.domain.intelligence_contracts import AgentRequest, AgentSpec, IntelligenceEvidence, IntelligenceExecutionContext
from cfip.domain.intelligence_runtime_policy import RuntimePolicy


def _request(*, evidence: int = 1, dry_run: bool = True, approval_required: bool = True) -> AgentRequest:
    items = tuple(IntelligenceEvidence(evidence_id=f"ev-{i}", source="test", kind="market", content_digest="digest", observed_at=1) for i in range(evidence))
    return AgentRequest(spec=AgentSpec(agent_id="agent", version="1", model_id="model"), prompt="analyze", context=IntelligenceExecutionContext(request_id="req", actor="test", purpose="analysis", evidence=items, dry_run=dry_run, approval_required=approval_required))


def test_policy_requires_evidence() -> None:
    assert RuntimePolicy().validate(_request(evidence=0)) == "evidence_required"


def test_policy_bounds_evidence() -> None:
    assert RuntimePolicy(max_evidence=1).validate(_request(evidence=2)) == "evidence_limit_exceeded"


def test_policy_blocks_live_execution() -> None:
    assert RuntimePolicy().validate(_request(dry_run=False)) == "approval_required_for_live_execution"


def test_service_uses_the_same_policy_semantics() -> None:
    assert IntelligenceRuntimeService.authorize(_request(evidence=0)).reason == "evidence_required"
