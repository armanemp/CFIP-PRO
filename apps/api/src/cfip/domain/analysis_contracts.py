"""Canonical analysis pipeline contract.

Adapters may use OSS indicator/analysis engines, but the platform exchanges only these
normalized structures. No provider-specific SDK type crosses this boundary.
"""

from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

AnalysisDirection = Literal["bullish", "bearish", "neutral"]
EvidenceKind = Literal["indicator", "structure", "order-flow", "market-data", "macro", "intelligence"]


class AnalysisEvidence(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    kind: EvidenceKind
    label: str = Field(min_length=1, max_length=256)
    direction: AnalysisDirection
    strength: float = Field(ge=0, le=1)
    source: str = Field(min_length=1, max_length=256)
    observed_at: int = Field(gt=0)


class AnalysisRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str = Field(min_length=1, max_length=64)
    timeframe: str = Field(min_length=1, max_length=16)
    evidence: list[AnalysisEvidence] = Field(default_factory=list, max_length=200)
    minimum_confluence: float = Field(ge=0, le=1, default=0.5)


class UnifiedAnalysis(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str
    timeframe: str
    direction: AnalysisDirection
    confidence: float = Field(ge=0, le=1)
    confluence: float = Field(ge=0, le=1)
    evidence: list[AnalysisEvidence] = Field(max_length=200)
    passed_gates: list[str] = Field(default_factory=list, max_length=50)
    blocked_reasons: list[str] = Field(default_factory=list, max_length=50)
    final_answer_available: bool


def aggregate_analysis(request: AnalysisRequest) -> UnifiedAnalysis:
    if not request.evidence:
        return UnifiedAnalysis(symbol=request.symbol, timeframe=request.timeframe, direction="neutral", confidence=0, confluence=0, evidence=[], blocked_reasons=["evidence_required"], final_answer_available=False)
    weighted = sum(item.strength for item in request.evidence)
    bullish = sum(item.strength for item in request.evidence if item.direction == "bullish")
    bearish = sum(item.strength for item in request.evidence if item.direction == "bearish")
    confluence = min(1.0, weighted / len(request.evidence))
    direction: AnalysisDirection = "bullish" if bullish > bearish else "bearish" if bearish > bullish else "neutral"
    confidence = min(1.0, abs(bullish - bearish) / max(weighted, 1e-12))
    passed = ["evidence_present"]
    blocked: list[str] = []
    if confluence < request.minimum_confluence:
        blocked.append("minimum_confluence_not_met")
    else:
        passed.append("minimum_confluence")
    return UnifiedAnalysis(symbol=request.symbol, timeframe=request.timeframe, direction=direction, confidence=confidence, confluence=confluence, evidence=request.evidence, passed_gates=passed, blocked_reasons=blocked, final_answer_available=not blocked)
