"""Evidence-first intelligence governance contracts.

Elyrava can propose changes, but application requires policy, authorization and verification.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

ProposalStatus = Literal["proposed","validated","awaiting-approval","approved","applied","verified","rejected","rolled-back"]

class EvidenceRef(BaseModel):
    model_config = ConfigDict(extra="forbid")
    source: str = Field(min_length=1, max_length=512)
    claim: str = Field(min_length=1, max_length=2000)
    collected_at: int = Field(gt=0)
    confidence: float = Field(ge=0, le=1)

class IntelligenceProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    objective: str = Field(min_length=1, max_length=2000)
    status: ProposalStatus = "proposed"
    risk: Literal["low","medium","high","critical"] = "medium"
    target_paths: tuple[str, ...] = ()
    validation_plan: tuple[str, ...] = ()
    rollback_plan: tuple[str, ...] = ()
    evidence: tuple[EvidenceRef, ...] = ()
    requires_human_approval: bool = True
    branch: str | None = None

class IntelligenceHealth(BaseModel):
    model_config = ConfigDict(extra="forbid")
    component: str
    status: Literal["healthy","degraded","failed","unknown"]
    score: float = Field(ge=0, le=1)
    evidence: tuple[str, ...] = ()
    last_verified_at: int | None = Field(default=None, gt=0)
