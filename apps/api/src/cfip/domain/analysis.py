"""Canonical backend analysis contract.

The API owns the normalized shape consumed by terminal, replay, alerts and future AI
orchestration. Provider-specific data and individual analyzers must not leak into it.
"""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

Bias = Literal["bullish", "bearish", "neutral"]
Recommendation = Literal["long", "short", "wait"]
Regime = Literal["trending", "ranging", "volatile", "mixed", "insufficient"]


class AnalysisEvidence(BaseModel):
    model_config = ConfigDict(extra="forbid")
    module: str
    bias: Bias
    score: float = Field(ge=-1, le=1)
    confidence: float = Field(ge=0, le=1)
    summary: str
    facts: list[str] = Field(default_factory=list)


class ConfluenceGate(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    passed: bool
    detail: str


class UnifiedAnalysisRead(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str
    timeframe: str
    as_of: str
    bias: Bias
    score: float = Field(ge=-1, le=1)
    confidence: float = Field(ge=0, le=1)
    regime: Regime
    recommendation: Recommendation
    modules: list[AnalysisEvidence] = Field(default_factory=list)
    confluence_score: int = Field(ge=0, le=100)
    confluence_threshold: int = Field(ge=0, le=100)
    confluence_accepted: bool
    gates: list[ConfluenceGate] = Field(default_factory=list)
    evidence: list[str] = Field(default_factory=list)
