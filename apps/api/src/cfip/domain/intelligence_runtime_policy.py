"""Reusable policy primitives for governed intelligence execution."""

from dataclasses import dataclass

from cfip.domain.intelligence_contracts import AgentRequest


@dataclass(frozen=True)
class RuntimePolicy:
    require_evidence: bool = True
    require_approval_for_live: bool = True
    require_model_identity: bool = True
    max_evidence: int = 64

    def validate(self, request: AgentRequest) -> str | None:
        context = request.context
        if self.require_evidence and not context.evidence:
            return "evidence_required"
        if len(context.evidence) > self.max_evidence:
            return "evidence_limit_exceeded"
        if self.require_approval_for_live and context.approval_required and not context.dry_run:
            return "approval_required_for_live_execution"
        if self.require_model_identity and not request.spec.model_id.strip():
            return "model_identity_required"
        return None
