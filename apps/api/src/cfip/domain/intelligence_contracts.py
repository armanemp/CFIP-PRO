"""Typed contracts for governed MIOSAI intelligence execution."""
from __future__ import annotations

from pydantic import BaseModel, ConfigDict, Field


class IntelligenceEvidence(BaseModel):
    model_config = ConfigDict(extra="forbid")

    evidence_id: str = Field(min_length=1, max_length=128)
    source: str = Field(min_length=1, max_length=256)
    kind: str = Field(min_length=1, max_length=64)
    content_digest: str = Field(min_length=1, max_length=128)
    observed_at: int = Field(gt=0)


class AgentSpec(BaseModel):
    model_config = ConfigDict(extra="forbid")

    agent_id: str = Field(min_length=1, max_length=128)
    version: str = Field(min_length=1, max_length=64)
    model_id: str = Field(min_length=1, max_length=128)


class IntelligenceExecutionContext(BaseModel):
    model_config = ConfigDict(extra="forbid")

    request_id: str = Field(min_length=1, max_length=128)
    actor: str = Field(min_length=1, max_length=128)
    purpose: str = Field(min_length=1, max_length=128)
    evidence: tuple[IntelligenceEvidence, ...] = ()
    dry_run: bool = True
    approval_required: bool = True


class AgentRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    spec: AgentSpec
    prompt: str = Field(min_length=1, max_length=20000)
    context: IntelligenceExecutionContext
