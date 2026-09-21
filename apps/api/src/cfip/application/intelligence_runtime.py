"""Deterministic governance gate for intelligence runtime execution."""

from dataclasses import dataclass

from cfip.application.intelligence_governance import authorize_agent
from cfip.domain.intelligence_contracts import AgentRequest, AgentResponse, AgentRuntime


@dataclass(frozen=True)
class RuntimeDecision:
    allowed: bool
    reason: str


class IntelligenceRuntimeService:
    """Keep provider execution behind evidence, scope and approval boundaries."""

    def __init__(self, agent: AgentRuntime) -> None:
        self.agent = agent

    @staticmethod
    def authorize(request: AgentRequest) -> RuntimeDecision:
        decision = authorize_agent(request)
        return RuntimeDecision(decision.allowed, decision.reason)

    async def run(self, request: AgentRequest) -> AgentResponse:
        decision = self.authorize(request)
        if not decision.allowed:
            raise PermissionError(decision.reason)
        return await self.agent.run(request)
