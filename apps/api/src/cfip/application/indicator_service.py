"""Backend-owned indicator registry and deterministic calculation service.

UI code consumes normalized series; it never owns indicator mathematics.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Callable

import numpy as np
import talib

from cfip.domain.indicator_contracts import IndicatorDefinition, IndicatorInstance, IndicatorPoint

Array = np.ndarray
Calculator = Callable[[Array, Array, Array, Array, Array, dict[str, float | int | str | bool]], dict[str, Array]]


@dataclass(frozen=True)
class IndicatorInput:
    time: tuple[int, ...]
    open: tuple[float, ...]
    high: tuple[float, ...]
    low: tuple[float, ...]
    close: tuple[float, ...]
    volume: tuple[float, ...]


def _period(params: dict[str, float | int | str | bool], key: str, default: int) -> int:
    value = params.get(key, default)
    if isinstance(value, bool):
        return default
    return max(1, int(value))


def _ema(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    return {"value": talib.EMA(c, timeperiod=_period(p, "period", 20))}


def _sma(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    return {"value": talib.SMA(c, timeperiod=_period(p, "period", 20))}


def _wma(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    return {"value": talib.WMA(c, timeperiod=_period(p, "period", 20))}


def _rsi(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    return {"value": talib.RSI(c, timeperiod=_period(p, "period", 14))}


def _atr(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    return {"value": talib.ATR(h, l, c, timeperiod=_period(p, "period", 14))}


def _adx(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    return {"value": talib.ADX(h, l, c, timeperiod=_period(p, "period", 14))}


def _obv(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    return {"value": talib.OBV(c, v)}


def _bbands(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    period = _period(p, "period", 20)
    std = float(p.get("std_dev", 2.0))
    upper, middle, lower = talib.BBANDS(c, timeperiod=period, nbdevup=std, nbdevdn=std)
    return {"upper": upper, "middle": middle, "lower": lower}


def _macd(o: Array, h: Array, l: Array, c: Array, v: Array, p: dict[str, float | int | str | bool]) -> dict[str, Array]:
    fast = _period(p, "fast", 12)
    slow = _period(p, "slow", 26)
    signal = _period(p, "signal", 9)
    macd, signal_line, histogram = talib.MACD(c, fastperiod=fast, slowperiod=slow, signalperiod=signal)
    return {"macd": macd, "signal": signal_line, "histogram": histogram}


_CALCULATORS: dict[str, Calculator] = {
    "ema": _ema, "sma": _sma, "wma": _wma, "rsi": _rsi, "atr": _atr,
    "adx": _adx, "obv": _obv, "bollinger": _bbands, "macd": _macd,
}

INDICATOR_CATALOG: tuple[IndicatorDefinition, ...] = (
    IndicatorDefinition(id="ema", name="Exponential Moving Average", kind="overlay", parameters={"period": 20}, output_names=("value",)),
    IndicatorDefinition(id="sma", name="Simple Moving Average", kind="overlay", parameters={"period": 20}, output_names=("value",)),
    IndicatorDefinition(id="wma", name="Weighted Moving Average", kind="overlay", parameters={"period": 20}, output_names=("value",)),
    IndicatorDefinition(id="rsi", name="Relative Strength Index", kind="oscillator", parameters={"period": 14}, output_names=("value",)),
    IndicatorDefinition(id="atr", name="Average True Range", kind="volatility", parameters={"period": 14}, output_names=("value",)),
    IndicatorDefinition(id="adx", name="Average Directional Index", kind="trend", parameters={"period": 14}, output_names=("value",)),
    IndicatorDefinition(id="obv", name="On-Balance Volume", kind="volume", parameters={}, output_names=("value",)),
    IndicatorDefinition(id="bollinger", name="Bollinger Bands", kind="volatility", parameters={"period": 20, "std_dev": 2.0}, output_names=("upper", "middle", "lower")),
    IndicatorDefinition(id="macd", name="MACD", kind="oscillator", parameters={"fast": 12, "slow": 26, "signal": 9}, output_names=("macd", "signal", "histogram")),
)


class IndicatorService:
    def definitions(self) -> tuple[IndicatorDefinition, ...]:
        return INDICATOR_CATALOG

    def calculate(self, instance: IndicatorInstance, data: IndicatorInput) -> tuple[IndicatorPoint, ...]:
        calculator = _CALCULATORS.get(instance.definition_id)
        if calculator is None:
            raise ValueError(f"unsupported_indicator:{instance.definition_id}")
        size = len(data.close)
        if not size or any(len(series) != size for series in (data.time, data.open, data.high, data.low, data.volume)):
            raise ValueError("invalid_ohlcv_lengths")
        outputs = calculator(
            np.asarray(data.open, dtype=np.float64), np.asarray(data.high, dtype=np.float64),
            np.asarray(data.low, dtype=np.float64), np.asarray(data.close, dtype=np.float64),
            np.asarray(data.volume, dtype=np.float64), instance.parameters,
        )
        points: list[IndicatorPoint] = []
        for i, timestamp in enumerate(data.time):
            values = {
                name: (float(series[i]) if np.isfinite(series[i]) else None)
                for name, series in outputs.items()
            }
            points.append(IndicatorPoint(time=timestamp, values=values))
        return tuple(points)
