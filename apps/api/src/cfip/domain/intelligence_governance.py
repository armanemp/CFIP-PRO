"""Governed self-development contracts for Elyrava.

This boundary makes platform self-improvement auditable and reversible. It does
not grant shell, production, or Git permissions by itself; adapters must enforce
those permissions separately.
"""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

ImprovementStage = Literal[
    "observe", "diagnose", "research", "propose", "validate",
    "approve", "promote", "observe_outcome", "learn",
]
ProposalRisk = Literal["low", "medium", "high", "critical"]
ProposalStatus = Literal[
    "proposed", "validation", "awaiting_approval", "approved",
    "promoted", "rejected", "rolled_back", "archived",
]


class EvidenceRef(BaseModel):
    model_config = ConfigDict(extra="forbid")

    evidence_id: str = Field(min_length=1, max_length=128)
    source: str = Field(min_length=1, max_length=256)
    content_hash: str = Field(min_length=16, max_length=128)
    observed_at: int = Field(gt=0)


class ValidationPlan(BaseModel):
    model_config = ConfigDict(extra="forbid")

    checks: list[str] = Field(min_length=1, max_length=50)
    acceptance_criteria: list[str] = Field(min_length=1, max_length=50)
    rollback_conditions: list[str] = Field(min_length=1, max_length=50)
    sandbox_required: bool = True


class ImprovementProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    stage: ImprovementStage = "propose"
    title: str = Field(min_length=1, max_length=256)
    rationale: str = Field(min_length=1, max_length=4000)
    risk: ProposalRisk
    evidence: list[EvidenceRef] = Field(min_length=1, max_length=100)
    validation: ValidationPlan
    git_paths: list[str] = Field(default_factory=list, max_length=100)
    status: ProposalStatus = "proposed"
    requires_human_approval: bool = True


class ValidationResult(BaseModel):
    model_config = ConfigDict(extra="forbid")

    proposal_id: str = Field(min_length=1, max_length=128)
    passed: bool
    checks: dict[str, bool] = Field(min_length=1, max_length=100)
    evidence_ids: list[str] = Field(min_length=1, max_length=100)
    rollback_required: bool = False
    summary: str = Field(min_length=1, max_length=4000)


class LearningOutcome(BaseModel):
    model_config = ConfigDict(extra="forbid")

    proposal_id: str = Field(min_length=1, max_length=128)
    outcome: Literal["improved", "neutral", "regressed", "unknown"]
    observed_at: int = Field(gt=0)
    metrics: dict[str, float] = Field(default_factory=dict, max_length=100)
    lesson: str = Field(min_length=1, max_length=4000)
