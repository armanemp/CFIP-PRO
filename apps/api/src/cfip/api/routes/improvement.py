"""Governed Aevrix improvement proposal endpoint.

This creates a proposal record in the request boundary only; persistence and promotion
must be connected to authenticated control-plane storage before production use.
"""
from fastapi import APIRouter
from cfip.application.self_improvement import evaluate_promotion_gate
from cfip.domain.self_improvement import ImprovementProposal

router = APIRouter(prefix="/improvement", tags=["intelligence"])

@router.post("/validate")
async def validate_proposal(proposal: ImprovementProposal) -> dict[str, object]:
    result = evaluate_promotion_gate(proposal)
    return {"proposal_id": proposal.id, "accepted_by_gate": result.accepted, "reasons": list(result.reasons), "mutation_performed": False}
