"""Tests for deterministic training-example preparation."""

from cfip.domain.analysis import AnalysisRequest, CandleInput
from cfip.domain.intelligence import IntelligenceEvidence
from cfip.infrastructure.analysis.engine import analyze
from cfip.infrastructure.intelligence.training import TrainingPreparationService


def _candles(count: int = 240) -> list[CandleInput]:
    out: list[CandleInput] = []
    price = 1.08
    for i in range(count):
        drift = 0.00008 if i % 7 else -0.00002
        close = price + drift
        out.append(
            CandleInput(
                time=1_700_000_000 + i * 900,
                open=price,
                high=max(price, close) + 0.00025,
                low=min(price, close) - 0.0002,
                close=close,
                volume=100 + i,
            )
        )
        price = close
    return out


def _evidence() -> list[IntelligenceEvidence]:
    return [
        IntelligenceEvidence(
            id="market-1",
            kind="market",
            source="reference",
            observed_at=1_700_000_000,
            content_hash="abcdef12",
            freshness_seconds=30,
            confidence=0.95,
        )
    ]


def test_training_example_uses_module_names_and_closed_bar_time() -> None:
    analysis = analyze(
        AnalysisRequest(
            symbol="EUR/USD",
            timeframe="15m",
            candles=_candles(),
        ),
        as_of="2026-09-18T00:00:00+00:00",
    )
    example = TrainingPreparationService.from_analysis(analysis, _evidence())
    assert example.as_of == analysis.closed_bar_time
    assert example.feature_vector["trend_score"] == next(
        item.score for item in analysis.modules if item.module == "trend"
    )


def test_training_example_rejects_missing_closed_bar_time() -> None:
    analysis = analyze(
        AnalysisRequest(
            symbol="EUR/USD",
            timeframe="15m",
            candles=_candles(),
        ),
        as_of="2026-09-18T00:00:00+00:00",
    ).model_copy(update={"closed_bar_time": None})
    try:
        TrainingPreparationService.from_analysis(analysis, _evidence())
    except ValueError as exc:
        assert str(exc) == "training_example_requires_closed_bar_time"
    else:
        raise AssertionError("training examples require a causal closed-bar timestamp")
