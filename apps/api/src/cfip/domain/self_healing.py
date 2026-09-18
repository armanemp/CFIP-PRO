"""Contracts for governed self-diagnosis and self-development.

The platform may detect, explain, test and propose repairs autonomously. Production
mutation is always represented as a governed proposal with explicit safety gates.
"""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

Severity = Literal["info", "warning", "critical"]
RepairAction = Literal[
    "restart",
    "rollback",
    "disable",
    "invalidate_cache",
    "rebuild_index",
    "code_change",
    "config_change",
]
ProposalStatus = Literal[
    "detected",
    "diagnosed",
    "testing",
    "awaiting_approval",
    "approved",
    "applied",
    "rolled_back",
    "rejected",
]


class HealthSignal(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    component: str = Field(min_length=1, max_length=256)
    metric: str = Field(min_length=1, max_length=128)
    value: float
    threshold: float
    severity: Severity
    observed_at: int = Field(gt=0)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)


class Diagnosis(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    component: str = Field(min_length=1, max_length=256)
    root_cause: str = Field(min_length=1, max_length=4000)
    confidence: float = Field(ge=0, le=1)
    evidence_ids: list[str] = Field(min_length=1, max_length=100)
    blast_radius: Literal["local", "component", "service", "platform"]
    reversible: bool


class RepairProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    diagnosis_id: str = Field(min_length=1, max_length=128)
    component: str = Field(min_length=1, max_length=256)
    action: RepairAction
    rationale: str = Field(min_length=1, max_length=4000)
    expected_effect: str = Field(min_length=1, max_length=2000)
    rollback_plan: str = Field(min_length=1, max_length=2000)
    status: ProposalStatus = "detected"
    requires_approval: bool = True
    can_auto_apply: bool = False


class VerificationResult(BaseModel):
    model_config = ConfigDict(extra="forbid")

    proposal_id: str
    passed: bool
    regression_count: int = Field(ge=0)
    safety_checks: list[str] = Field(default_factory=list, max_length=100)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)


class SelfHealingPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")

    allow_auto_apply_reversible: bool = False
    require_rollback_plan: bool = True
    require_verification: bool = True
    max_blast_radius: Literal["local", "component"] = "local"
