"""Deterministic governance gate for intelligence runtime execution."""

from dataclasses import dataclass

from cfip.domain.intelligence_contracts import AgentRequest, AgentResponse, AgentRuntime


@dataclass(frozen=True)
class RuntimeDecision:
    allowed: bool
    reason: str


class IntelligenceRuntimeService:
    """Keep provider execution behind evidence and approval boundaries."""

    def __init__(self, agent: AgentRuntime) -> None:
        self.agent = agent

    @staticmethod
    def authorize(request: AgentRequest) -> RuntimeDecision:
        context = request.context
        if not context.evidence:
            return RuntimeDecision(False, "evidence_required")
        if context.approval_required and not context.dry_run:
            return RuntimeDecision(False, "approval_required_for_live_execution")
        return RuntimeDecision(True, "allowed")

    async def run(self, request: AgentRequest) -> AgentResponse:
        decision = self.authorize(request)
        if not decision.allowed:
            raise PermissionError(decision.reason)
        return await self.agent.run(request)
