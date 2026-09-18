"""Tests for the canonical analysis boundary."""

from cfip.domain.analysis import AnalysisRequest, CandleInput
from cfip.infrastructure.analysis.engine import analyze


def _candles(count: int = 240) -> list[CandleInput]:
    out: list[CandleInput] = []
    price = 1.08
    for i in range(count):
        drift = 0.00008 if i % 7 else -0.00002
        close = price + drift
        high = max(price, close) + 0.00025
        low = min(price, close) - 0.0002
        out.append(
            CandleInput(
                time=1_700_000_000 + i * 900,
                open=price,
                high=high,
                low=low,
                close=close,
                volume=100 + i,
            )
        )
        price = close
    return out


def test_analysis_is_closed_bar_causal() -> None:
    base = _candles()
    request = AnalysisRequest(symbol="EUR/USD", timeframe="15m", candles=base)
    first = analyze(request, as_of="2026-09-18T00:00:00+00:00")
    mutated = list(base)
    mutated[-1] = CandleInput(
        time=base[-1].time,
        open=100,
        high=101,
        low=99,
        close=100,
        volume=999999,
    )
    second = analyze(request.model_copy(update={"candles": mutated}), as_of=first.as_of)
    assert first.closed_bar_time == second.closed_bar_time
    assert first.score == second.score
    assert first.recommendation == second.recommendation


def test_analysis_waits_when_confluence_is_not_accepted() -> None:
    request = AnalysisRequest(
        symbol="EUR/USD",
        timeframe="15m",
        candles=_candles(80),
        min_confluence_score=100,
    )
    result = analyze(request, as_of="2026-09-18T00:00:00+00:00")
    assert result.recommendation == "wait"
    assert not result.confluence_accepted


def test_risk_is_not_fabricated_without_account_context() -> None:
    result = analyze(
        AnalysisRequest(symbol="EUR/USD", timeframe="15m", candles=_candles()),
        as_of="2026-09-18T00:00:00+00:00",
    )
    assert result.risk_target.available is False
    assert result.risk_target.entry is None


def test_analysis_exposes_multi_timeframe_context() -> None:
    result = analyze(
        AnalysisRequest(symbol="EUR/USD", timeframe="15m", candles=_candles(1000), minimum_aligned_htfs=2),
        as_of="2026-09-18T00:00:00+00:00",
    )
    assert [item.timeframe for item in result.mtf_contexts] == ["1H", "4H", "1D"]
    assert all(item.closed_bar_time <= result.closed_bar_time for item in result.mtf_contexts)
    assert all(0 <= item.completeness <= 1 for item in result.mtf_contexts)


def test_analysis_respects_minimum_aligned_htfs():
    request = AnalysisRequest(symbol="EUR/USD", timeframe="15m", candles=_candles(), minimum_aligned_htfs=2)
    result = analyze(request, as_of="2026-09-18T00:00:00+00:00")
    gate = next(g for g in result.gates if g.id == "htf_alignment")
    assert gate.passed is False


def test_analysis_rejects_insufficient_closed_history() -> None:
    request = AnalysisRequest(
        symbol="EUR/USD",
        timeframe="15m",
        candles=_candles(5),
    )
    try:
        analyze(request, as_of="2026-09-18T00:00:00+00:00")
    except ValueError as exc:
        assert str(exc) == "insufficient_closed_bars"
    else:
        raise AssertionError("analysis must reject fewer than five closed bars")


def test_analysis_exposes_lifecycle_state_contracts() -> None:
    result = analyze(
        AnalysisRequest(symbol="EUR/USD", timeframe="15m", candles=_candles()),
        as_of="2026-09-18T00:00:00+00:00",
    )
    assert all(item.origin_time <= item.last_evaluated_time for item in result.fvg_states)
    assert all(item.origin_time <= item.last_evaluated_time for item in result.order_blocks)
    assert all(0 <= item.mitigation_ratio <= 1 for item in result.fvg_states)


def test_analysis_contract_rejects_invalid_ohlc() -> None:
    try:
        CandleInput(time=1, open=2, high=1, low=0, close=2)
    except ValueError as exc:
        assert "invalid_ohlc" in str(exc)
    else:
        raise AssertionError("invalid OHLC must be rejected")
