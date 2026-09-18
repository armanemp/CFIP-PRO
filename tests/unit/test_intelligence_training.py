"""Regression tests for governed intelligence training preparation."""

from cfip.domain.analysis import AnalysisRequest
from cfip.domain.intelligence import IntelligenceEvidence
from cfip.infrastructure.analysis.engine import analyze
from cfip.infrastructure.intelligence.training import TrainingPreparationService

from test_analysis_engine import _candles


def test_training_example_is_derived_from_canonical_analysis() -> None:
    analysis = analyze(
        AnalysisRequest(symbol="EUR/USD", timeframe="15m", candles=_candles(240)),
        as_of="2026-09-18T00:00:00+00:00",
    )
    evidence = [
        IntelligenceEvidence(
            id="market-1",
            kind="market",
            source="reference",
            observed_at=1,
            content_hash="abcdef12",
            freshness_seconds=30,
            confidence=0.9,
        )
    ]
    example = TrainingPreparationService().from_analysis(
        analysis, evidence=evidence, example_id="example-1"
    )
    assert example.target == analysis.bias
    assert example.feature_vector["analysis_score"] == analysis.score
    assert example.evidence_ids == ["market-1"]
    assert example.label_quality == 0.9
