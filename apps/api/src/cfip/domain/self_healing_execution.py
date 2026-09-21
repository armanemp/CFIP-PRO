"""Governed execution contracts for self-healing and self-development.

No process, shell, network, or production mutation is performed here.
"""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

ExecutionStatus = Literal["blocked", "ready", "executed", "failed", "rolled_back"]
RiskLevel = Literal["low", "medium", "high", "critical"]
ArtifactKind = Literal[
    "config", "cache", "index", "service", "code", "dependency", "documentation"
]


class SafetyInvariant(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    description: str = Field(min_length=1, max_length=1000)
    required: bool = True


class RepairExecutionRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    proposal_id: str = Field(min_length=1, max_length=128)
    artifact_kind: ArtifactKind
    risk: RiskLevel = "low"
    artifact_digest: str = Field(min_length=16, max_length=128)
    rollback_digest: str = Field(min_length=16, max_length=128)
    test_evidence_ids: list[str] = Field(min_length=1, max_length=100)
    security_evidence_ids: list[str] = Field(default_factory=list, max_length=100)
    safety_invariant_ids: list[str] = Field(min_length=1, max_length=100)
    canary_required: bool = True
    approval_id: str | None = Field(default=None, max_length=128)
    status: ExecutionStatus = "blocked"


class SelfDevelopmentChange(BaseModel):
    model_config = ConfigDict(extra="forbid")

    change_id: str = Field(min_length=1, max_length=128)
    branch_ref: str = Field(min_length=1, max_length=256)
    artifact_digest: str = Field(min_length=16, max_length=128)
    test_evidence_ids: list[str] = Field(min_length=1, max_length=100)
    security_evidence_ids: list[str] = Field(default_factory=list, max_length=100)
    risk: RiskLevel = "low"
    review_required: bool = False
    production_apply_allowed: bool = True


class ExecutionGateResult(BaseModel):
    model_config = ConfigDict(extra="forbid")

    allowed: bool
    reasons: list[str] = Field(default_factory=list, max_length=50)


class SelfHealingExecutionPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")

    require_approval_for_code: bool = False
    require_approval_for_dependency: bool = False
    require_signed_artifact: bool = True
    require_rollback_digest: bool = True
    require_canary: bool = True
    require_security_evidence_for_mutation: bool = True
    min_test_evidence: int = Field(default=1, ge=1, le=100)
    max_consecutive_repairs: int = Field(default=3, ge=1, le=100)
    cooldown_seconds: int = Field(default=300, ge=0, le=86400)
    circuit_breaker_after_failures: int = Field(default=3, ge=1, le=100)

    @model_validator(mode="after")
    def validate_budget(self) -> "SelfHealingExecutionPolicy":
        if self.max_consecutive_repairs < self.circuit_breaker_after_failures:
            raise ValueError("repair_budget_below_circuit_breaker")
        return self
