"""Governed self-development contracts for MIOS.

The platform can observe, diagnose and propose improvements. Promotion remains a
separate authorization boundary and must carry evidence plus rollback information.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

ProposalKind = Literal["bug-fix","performance","security","data-quality","model","ux","dependency"]

class ImprovementProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    kind: ProposalKind
    title: str = Field(min_length=1, max_length=200)
    rationale: str = Field(min_length=1, max_length=4000)
    evidence_ids: tuple[str, ...] = ()
    affected_paths: tuple[str, ...] = ()
    validation_plan: tuple[str, ...] = ()
    rollback_plan: tuple[str, ...] = ()
    requires_approval: bool = True
    risk: Literal["low","medium","high","critical"] = "medium"

class PromotionDecision(BaseModel):
    model_config = ConfigDict(extra="forbid")
    proposal_id: str
    approved: bool
    actor: str
    reason: str = Field(default="", max_length=2000)
    validation_run_id: str | None = None
