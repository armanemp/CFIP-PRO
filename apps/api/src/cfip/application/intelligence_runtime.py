"""Governed runtime authorization boundary for intelligence execution."""
from dataclasses import dataclass

from cfip.domain.intelligence_contracts import AgentRequest
from cfip.domain.intelligence_runtime_policy import RuntimePolicy


@dataclass(frozen=True)
class AuthorizationDecision:
    allowed: bool
    reason: str | None = None


class IntelligenceRuntimeService:
    """Centralizes runtime authorization before an agent is allowed to execute."""

    policy = RuntimePolicy()

    @classmethod
    def authorize(cls, request: AgentRequest) -> AuthorizationDecision:
        reason = cls.policy.validate(request)
        return AuthorizationDecision(allowed=reason is None, reason=reason)
