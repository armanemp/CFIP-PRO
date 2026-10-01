"""Deterministic governance gates for autonomous improvement proposals."""
from cfip.domain.self_improvement import ImprovementProposal

class PromotionGateResult:
    def __init__(self, accepted: bool, reasons: tuple[str, ...]) -> None:
        self.accepted = accepted
        self.reasons = reasons

def evaluate_promotion_gate(proposal: ImprovementProposal) -> PromotionGateResult:
    reasons: list[str] = []
    if not proposal.evidence_ids:
        reasons.append("missing_evidence")
    if not proposal.validation_plan:
        reasons.append("missing_validation_plan")
    if not proposal.rollback_plan:
        reasons.append("missing_rollback_plan")
    if proposal.risk in {"high", "critical"}:
        reasons.append("human_approval_required")
    return PromotionGateResult(accepted=not reasons, reasons=tuple(reasons))
