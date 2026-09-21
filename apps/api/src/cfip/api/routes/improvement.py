"""Governed MIOS improvement proposal endpoint.

This endpoint validates a proposal at the request boundary only. It never mutates
source code, configuration, models or deployment state. Persistence and promotion
remain behind the authenticated control-plane boundary.
"""

from fastapi import APIRouter

from cfip.application.self_improvement import evaluate_promotion_gate
from cfip.domain.self_improvement import ImprovementProposal, PromotionDecision

router = APIRouter(prefix="/improvement", tags=["intelligence"])


@router.post("/validate")
async def validate_proposal(proposal: ImprovementProposal) -> dict[str, object]:
    result = evaluate_promotion_gate(proposal)
    return {
        "proposal_id": proposal.id,
        "fingerprint": proposal.fingerprint(),
        "accepted_by_gate": result.accepted,
        "reasons": list(result.reasons),
        "mutation_performed": False,
    }


@router.post("/decision")
async def validate_decision(decision: PromotionDecision) -> dict[str, object]:
    """Normalize a governance decision without granting promotion authority."""
    return {
        "proposal_id": decision.proposal_id,
        "approved": decision.approved,
        "actor": decision.actor,
        "validation_run_id": decision.validation_run_id,
        "mutation_performed": False,
    }
