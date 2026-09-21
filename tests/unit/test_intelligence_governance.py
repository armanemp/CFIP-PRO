from cfip.application.intelligence_governance import authorize_agent, authorize_tool
from cfip.domain.intelligence_contracts import (
    AgentRequest, AgentSpec, IntelligenceExecutionContext, IntelligenceEvidence,
    ToolRequest, ToolSpec,
)


def ctx(*, evidence=True, dry_run=True, approval=True):
    return IntelligenceExecutionContext(
        request_id="r1", actor="test", purpose="analysis",
        evidence=(IntelligenceEvidence(evidence_id="e1", source="test", kind="market", content_digest="sha256:1234567890123456", observed_at=1),) if evidence else (),
        dry_run=dry_run, approval_required=approval,
    )


def test_agent_requires_evidence():
    request = AgentRequest(spec=AgentSpec(agent_id="a", version="1", model_id="m"), prompt="x", context=ctx(evidence=False))
    assert authorize_agent(request).reason == "evidence_required"


def test_side_effect_tool_requires_evidence():
    request = ToolRequest(spec=ToolSpec(tool_id="trade", version="1", description="trade", side_effects=True), arguments={}, context=ctx(evidence=False))
    decision = authorize_tool(request)
    assert not decision.allowed
    assert decision.reason == "evidence_required_for_side_effect"


def test_read_only_dry_run_tool_is_allowed():
    request = ToolRequest(spec=ToolSpec(tool_id="quote", version="1", description="quote", side_effects=False), arguments={}, context=ctx())
    assert authorize_tool(request).allowed
