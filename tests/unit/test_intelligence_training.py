"""Regression tests for governed intelligence training preparation."""

from cfip.domain.analysis import AnalysisRequest
from cfip.domain.intelligence import IntelligenceEvidence
from cfip.infrastructure.analysis.engine import analyze
from cfip.infrastructure.intelligence.training import TrainingPreparationService

def _candles(count: int = 240):
    from cfip.domain.analysis import CandleInput
    price = 1.08
    result = []
    for index in range(count):
        close = price + (0.00008 if index % 7 else -0.00002)
        result.append(CandleInput(
            time=1_700_000_000 + index * 900,
            open=price,
            high=max(price, close) + 0.00025,
            low=min(price, close) - 0.00020,
            close=close,
            volume=100 + index,
        ))
        price = close
    return result




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
