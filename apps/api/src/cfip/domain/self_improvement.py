"""Governed self-development contracts for MIOS.

MIOS can observe, diagnose and propose improvements. Promotion remains a separate
authorization boundary and must carry evidence, validation and rollback information.
The contracts are deterministic and fail closed for material changes.
"""

from hashlib import sha256
from json import dumps
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

ProposalKind = Literal["bug-fix", "performance", "security", "data-quality", "model", "ux", "dependency"]
ProposalRisk = Literal["low", "medium", "high", "critical"]


class ImprovementProposal(BaseModel):
    model_config = ConfigDict(extra="forbid", frozen=True)

    id: str = Field(min_length=1, max_length=128)
    kind: ProposalKind
    title: str = Field(min_length=1, max_length=200)
    rationale: str = Field(min_length=1, max_length=4000)
    evidence_ids: tuple[str, ...] = ()
    affected_paths: tuple[str, ...] = ()
    validation_plan: tuple[str, ...] = ()
    rollback_plan: tuple[str, ...] = ()
    requires_approval: bool = True
    risk: ProposalRisk = "medium"

    def governance_issues(self) -> tuple[str, ...]:
        """Return deterministic fail-closed reasons before promotion."""
        reasons: list[str] = []
        if not self.evidence_ids:
            reasons.append("missing_evidence")
        if not self.validation_plan:
            reasons.append("missing_validation_plan")
        if self.risk != "low" and not self.rollback_plan:
            reasons.append("missing_rollback_plan")
        if len(set(self.evidence_ids)) != len(self.evidence_ids):
            reasons.append("duplicate_evidence")
        if len(set(self.affected_paths)) != len(self.affected_paths):
            reasons.append("duplicate_paths")
        if self.risk in {"high", "critical"} and not self.requires_approval:
            reasons.append("human_approval_required")
        return tuple(reasons)

    def fingerprint(self) -> str:
        """Return a deterministic identity for the immutable proposal payload."""
        payload = self.model_dump(mode="json")
        return sha256(
            dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8")
        ).hexdigest()


class PromotionDecision(BaseModel):
    model_config = ConfigDict(extra="forbid", frozen=True)

    proposal_id: str = Field(min_length=1, max_length=128)
    approved: bool
    actor: str = Field(min_length=1, max_length=128)
    reason: str = Field(default="", max_length=2000)
    validation_run_id: str | None = Field(default=None, min_length=1, max_length=128)

    @model_validator(mode="after")
    def enforce_validation_before_promotion(self) -> "PromotionDecision":
        if self.approved and not self.validation_run_id:
            raise ValueError("approval_requires_validation_run")
        if not self.approved and not self.reason.strip():
            raise ValueError("rejection_requires_reason")
        return self
