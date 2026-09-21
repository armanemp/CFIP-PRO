"""Canonical contracts for governed platform intelligence and learning."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

LearningType = Literal[
    "market_pattern",
    "trade_outcome",
    "user_feedback",
    "research",
    "data_quality",
    "model_evaluation",
    "risk_review",
    "system_incident",
]
EvidenceKind = Literal["market", "analysis", "outcome", "research", "user", "system"]
LessonStatus = Literal["candidate", "validated", "rejected", "superseded"]
ProposalKind = Literal["strategy", "threshold", "data_source", "prompt", "workflow", "code"]


class IntelligenceEvidence(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    kind: EvidenceKind
    source: str = Field(min_length=1, max_length=256)
    observed_at: int = Field(gt=0)
    content_hash: str = Field(min_length=8, max_length=128)
    freshness_seconds: int = Field(ge=0)
    confidence: float = Field(ge=0, le=1)
    facts: list[str] = Field(default_factory=list, max_length=100)


class LearningRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    type: LearningType
    created_at: int = Field(gt=0)
    subject: str = Field(min_length=1, max_length=256)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)
    lesson: str = Field(min_length=1, max_length=4000)
    confidence: float = Field(ge=0, le=1)
    status: LessonStatus = "candidate"
    validation_count: int = Field(default=0, ge=0)

    @model_validator(mode="after")
    def require_evidence_for_learning(self) -> "LearningRecord":
        if (
            self.type
            in {"market_pattern", "trade_outcome", "research", "model_evaluation"}
            and not self.evidence_ids
        ):
            raise ValueError("learning_requires_evidence")
        return self


class TrainingExample(BaseModel):
    """Leakage-resistant, lineage-aware example prepared for model evaluation/training."""

    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    subject: str = Field(min_length=1, max_length=256)
    schema_version: str = Field(default="1", min_length=1, max_length=32)
    as_of: int = Field(gt=0)
    timeframe: str = Field(min_length=1, max_length=16)
    feature_vector: dict[str, float] = Field(default_factory=dict, max_length=128)
    feature_schema: dict[str, str] = Field(default_factory=dict, max_length=128)
    target: Literal["bullish", "bearish", "neutral"] | None = None
    outcome: Literal["win", "loss", "breakeven", "unknown"] = "unknown"
    label_horizon_bars: int = Field(default=0, ge=0, le=100000)
    label_quality: float = Field(default=0.0, ge=0, le=1)
    evidence_ids: list[str] = Field(min_length=1, max_length=100)
    source_analysis_id: str | None = Field(default=None, max_length=128)
    dataset_id: str | None = Field(default=None, max_length=128)
    provenance_hash: str = Field(min_length=16, max_length=128)

    @model_validator(mode="after")
    def validate_lineage(self) -> "TrainingExample":
        if not self.feature_vector:
            raise ValueError("training_example_requires_features")
        if set(self.feature_schema) != set(self.feature_vector):
            raise ValueError("training_feature_schema_mismatch")
        return self


class IntelligenceFeedback(BaseModel):
    model_config = ConfigDict(extra="forbid")

    learning_id: str = Field(min_length=1, max_length=128)
    accepted: bool
    reviewer: str = Field(min_length=1, max_length=128)
    reason: str = Field(min_length=1, max_length=2000)
    observed_at: int = Field(gt=0)


class IntelligenceProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str = Field(min_length=1, max_length=128)
    kind: ProposalKind
    title: str = Field(min_length=1, max_length=256)
    rationale: str = Field(min_length=1, max_length=4000)
    evidence_ids: list[str] = Field(min_length=1, max_length=100)
    risk_level: Literal["low", "medium", "high"]
    requires_approval: bool = True
    status: Literal["proposed", "approved", "rejected", "archived"] = "proposed"


class IntelligenceSnapshot(BaseModel):
    model_config = ConfigDict(extra="forbid")

    as_of: int = Field(gt=0)
    evidence_count: int = Field(ge=0)
    active_lessons: int = Field(ge=0)
    validated_lessons: int = Field(ge=0)
    open_proposals: int = Field(ge=0)
    calibration_score: float | None = Field(default=None, ge=0, le=1)
