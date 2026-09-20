from typing import Literal

from pydantic import BaseModel, ConfigDict, Field


class LearningObservation(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    source: Literal["market", "analysis", "execution", "outcome", "user_feedback", "research"]
    occurred_at: int = Field(gt=0)
    feature_version: str = Field(min_length=1, max_length=80)
    payload_hash: str = Field(min_length=8, max_length=256)
    outcome_label: str | None = Field(default=None, max_length=200)


class LearningEvaluation(BaseModel):
    model_config = ConfigDict(extra="forbid")
    evaluation_id: str = Field(min_length=1, max_length=128)
    model_version: str = Field(min_length=1, max_length=128)
    dataset_version: str = Field(min_length=1, max_length=128)
    metrics: dict[str, float] = Field(default_factory=dict)
    drift_detected: bool = False
    passed: bool = False
    rollback_required: bool = False
