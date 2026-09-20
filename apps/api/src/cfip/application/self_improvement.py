"""Deterministic governance gates for autonomous improvement proposals."""

from cfip.domain.self_improvement import ImprovementProposal


class PromotionGateResult:
    def __init__(self, accepted: bool, reasons: tuple[str, ...]) -> None:
        self.accepted = accepted
        self.reasons = reasons


def evaluate_promotion_gate(proposal: ImprovementProposal) -> PromotionGateResult:
    reasons = proposal.governance_issues()
    return PromotionGateResult(accepted=not reasons, reasons=reasons)
