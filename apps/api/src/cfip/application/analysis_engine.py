"""Composable evidence engine.

Each analyzer is a replaceable module. The engine only aggregates normalized evidence.
"""
from __future__ import annotations
from dataclasses import dataclass
from typing import Protocol
from cfip.domain.analysis_contracts import AnalysisEvidence, AnalysisKind, ConsensusResult

class Analyzer(Protocol):
    kind: AnalysisKind
    def analyze(self, symbol: str, timeframe: str, bars: list[dict[str, float | int]]) -> AnalysisEvidence | None: ...

@dataclass(frozen=True)
class AnalysisEngine:
    analyzers: tuple[Analyzer, ...]

    def run(self, symbol: str, timeframe: str, bars: list[dict[str, float | int]], generated_at: int) -> tuple[tuple[AnalysisEvidence, ...], ConsensusResult]:
        evidence = tuple(item for analyzer in self.analyzers if (item := analyzer.analyze(symbol, timeframe, bars)) is not None)
        if not evidence:
            direction = "neutral"
            confidence = 0.0
        else:
            weights = {"bullish": 1.0, "bearish": -1.0, "neutral": 0.0, "mixed": 0.0}
            total = sum(item.confidence for item in evidence) or 1.0
            score = sum(weights[item.direction] * item.confidence for item in evidence) / total
            direction = "bullish" if score > 0.2 else "bearish" if score < -0.2 else "neutral"
            confidence = min(1.0, abs(score))
        return evidence, ConsensusResult(
            symbol=symbol, timeframe=timeframe, direction=direction,
            confidence=confidence, evidence_ids=tuple(item.id for item in evidence),
            generated_at=generated_at,
        )
