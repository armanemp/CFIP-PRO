"""Regression tests for causal higher-timeframe and FVG analysis."""

from types import SimpleNamespace

import numpy as np

from cfip.infrastructure.analysis.engine import _aggregate_htf, _fvg_lifecycle


def _candles(count: int, *, base: int = 900) -> list[SimpleNamespace]:
    return [
        SimpleNamespace(
            time=i * base,
            open=1.1,
            high=1.101,
            low=1.099,
            close=1.1,
            volume=1.0,
        )
        for i in range(count)
    ]


def test_htf_aggregation_rejects_gaps() -> None:
    candles = _candles(4)
    candles.pop(3)
    bars, completeness = _aggregate_htf(candles, 900, 3600)
    assert bars == []
    assert completeness == 0.0


def test_fvg_invalidates_on_close_through_boundary() -> None:
    high = np.array([1.1000, 1.1005, 1.1002, 1.1010], dtype=np.float64)
    low = np.array([1.0990, 1.0995, 1.1008, 1.0980], dtype=np.float64)
    times = np.array([900, 1800, 2700, 3600], dtype=np.int64)
    close = np.array([1.1000, 1.1000, 1.1009, 1.0990], dtype=np.float64)
    states = _fvg_lifecycle(high, low, close, times)
    assert any(item["state"] == "invalidated" for item in states)
