"""Shared authorization rules for agent and tool execution."""

from dataclasses import dataclass

from cfip.domain.intelligence_contracts import (
    AgentRequest,
    ToolRequest,
)


@dataclass(frozen=True)
class GovernanceDecision:
    allowed: bool
    reason: str


def authorize_agent(request: AgentRequest) -> GovernanceDecision:
    context = request.context
    if not context.evidence:
        return GovernanceDecision(False, "evidence_required")
    if not request.spec.model_id.strip():
        return GovernanceDecision(False, "model_identity_required")
    if context.approval_required and not context.dry_run:
        return GovernanceDecision(False, "approval_required_for_live_execution")
    return GovernanceDecision(True, "allowed")


def authorize_tool(request: ToolRequest) -> GovernanceDecision:
    context = request.context
    if request.spec.side_effects and not context.evidence:
        return GovernanceDecision(False, "evidence_required_for_side_effect")
    if request.spec.approval_required and not context.dry_run:
        return GovernanceDecision(False, "approval_required_for_tool")
    return GovernanceDecision(True, "allowed")
