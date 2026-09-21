"""Governed self-development state machine.

The platform may discover and validate improvements automatically, but promotion
is an explicit governance transition. No proposal can jump directly to applied.
"""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

ImprovementStage = Literal[
    "observed",
    "diagnosed",
    "proposed",
    "validated",
    "approved",
    "applied",
    "rolled_back",
    "rejected",
]


class ImprovementEvidence(BaseModel):
    model_config = ConfigDict(extra="forbid")

    evidence_id: str = Field(min_length=1, max_length=128)
    digest: str = Field(min_length=16, max_length=128)
    source: str = Field(min_length=1, max_length=256)
    observed_at: int = Field(gt=0)


class ImprovementProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")

    proposal_id: str = Field(min_length=1, max_length=128)
    component: str = Field(min_length=1, max_length=256)
    change_type: Literal["config", "prompt", "workflow", "code", "model", "data_source"]
    stage: ImprovementStage = "observed"
    rationale: str = Field(min_length=1, max_length=4000)
    evidence: list[ImprovementEvidence] = Field(min_length=1, max_length=100)
    test_plan: list[str] = Field(min_length=1, max_length=50)
    rollback_plan: str = Field(min_length=1, max_length=4000)
    risk_level: Literal["low", "medium", "high"]
    approval_required: bool = True
    approved_by: str | None = Field(default=None, max_length=128)
    applied_revision: str | None = Field(default=None, max_length=128)

    @model_validator(mode="after")
    def enforce_governance(self) -> "ImprovementProposal":
        if self.stage in {"approved", "applied"} and not self.approved_by:
            raise ValueError("promotion_requires_explicit_approval")
        if self.stage == "applied" and not self.applied_revision:
            raise ValueError("applied_improvement_requires_revision")
        if self.stage == "validated" and not self.test_plan:
            raise ValueError("validated_improvement_requires_test_plan")
        return self


class ImprovementTransition(BaseModel):
    model_config = ConfigDict(extra="forbid")

    proposal_id: str = Field(min_length=1, max_length=128)
    from_stage: ImprovementStage
    to_stage: ImprovementStage
    actor: str = Field(min_length=1, max_length=128)
    occurred_at: int = Field(gt=0)
    evidence_ids: list[str] = Field(min_length=1, max_length=100)
    revision: str | None = Field(default=None, max_length=128)
    reason: str = Field(min_length=1, max_length=2000)

    @model_validator(mode="after")
    def prevent_unsafe_transition(self) -> "ImprovementTransition":
        if self.to_stage == "applied" and self.from_stage != "approved":
            raise ValueError("applied_requires_approved_stage")
        if self.to_stage == "approved" and self.from_stage != "validated":
            raise ValueError("approved_requires_validated_stage")
        if self.to_stage == "rolled_back" and not self.revision:
            raise ValueError("rollback_requires_revision")
        return self
