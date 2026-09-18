"""Deterministic market-analysis engine.

Classical indicators are delegated to TA-Lib. CFIP-specific SMC/state logic remains
small and explicit because FVG, liquidity, structure and confluence are domain rules,
not generic indicator calculations.
"""

from dataclasses import dataclass
from math import isfinite
from typing import Sequence

import numpy as np
import talib

from cfip.domain.analysis import (
    AnalysisEvidence,
    AnalysisRequest,
    Bias,
    ConfluenceGate,
    RiskTargetPlan,
    UnifiedAnalysisRead,
)


@dataclass(frozen=True)
class _Module:
    name: str
    score: float
    confidence: float
    summary: str
    facts: tuple[str, ...]


def _arr(request: AnalysisRequest) -> tuple[np.ndarray, ...]:
    c = request.candles
    return tuple(
        np.asarray([getattr(x, field) for x in c], dtype=np.float64)
        for field in ("open", "high", "low", "close", "volume")
    )


def _last(value: np.ndarray) -> float | None:
    if value.size == 0 or not isfinite(float(value[-1])):
        return None
    return float(value[-1])


def _bias(score: float) -> Bias:
    if score > 0.15:
        return "bullish"
    if score < -0.15:
        return "bearish"
    return "neutral"


def _module_evidence(module: _Module) -> AnalysisEvidence:
    return AnalysisEvidence(
        module=module.name,
        bias=_bias(module.score),
        score=max(-1.0, min(1.0, module.score)),
        confidence=max(0.0, min(1.0, module.confidence)),
        summary=module.summary,
        facts=list(module.facts),
    )


def _swing_state(high: np.ndarray, low: np.ndarray, close: np.ndarray) -> tuple[float, float, str]:
    if len(close) < 7:
        return 0.0, 0.0, "insufficient"
    highs = [
        high[i] for i in range(2, len(close) - 2)
        if high[i] > high[i - 1] and high[i] >= high[i + 1]
    ]
    lows = [
        low[i] for i in range(2, len(close) - 2)
        if low[i] < low[i - 1] and low[i] <= low[i + 1]
    ]
    if len(highs) < 2 or len(lows) < 2:
        return 0.0, 0.35, "insufficient"
    hh = highs[-1] > highs[-2]
    hl = lows[-1] > lows[-2]
    lh = highs[-1] < highs[-2]
    ll = lows[-1] < lows[-2]
    score = (1 if hh else 0) + (1 if hl else 0) - (1 if lh else 0) - (1 if ll else 0)
    regime = "trending" if abs(score) >= 2 else "mixed"
    return score / 2.0, 0.78, regime


def _fvg_state(high: np.ndarray, low: np.ndarray) -> tuple[float, bool, str]:
    bullish = False
    bearish = False
    for i in range(2, len(high)):
        if high[i - 2] < low[i]:
            bullish = True
        if low[i - 2] > high[i]:
            bearish = True
    if bullish and not bearish:
        return 0.65, True, "recent bullish fair-value gap"
    if bearish and not bullish:
        return -0.65, True, "recent bearish fair-value gap"
    if bullish and bearish:
        return 0.0, True, "mixed fair-value-gap context"
    return 0.0, False, "no recent fair-value gap"


def _liquidity_state(high: np.ndarray, low: np.ndarray, close: np.ndarray, atr: float) -> tuple[float, bool, str]:
    if len(close) < 8 or atr <= 0:
        return 0.0, False, "insufficient liquidity history"
    tolerance = max(atr * 0.15, close[-1] * 0.00005)
    equal_high = abs(high[-2] - high[-4]) <= tolerance
    equal_low = abs(low[-2] - low[-4]) <= tolerance
    sweep_high = high[-1] > max(high[-2], high[-3]) + tolerance and close[-1] < high[-2]
    sweep_low = low[-1] < min(low[-2], low[-3]) - tolerance and close[-1] > low[-2]
    if sweep_low:
        return 0.7, True, "sell-side liquidity sweep with reclaim"
    if sweep_high:
        return -0.7, True, "buy-side liquidity sweep with rejection"
    if equal_low and not equal_high:
        return 0.25, True, "sell-side equal-low liquidity pool"
    if equal_high and not equal_low:
        return -0.25, True, "buy-side equal-high liquidity pool"
    return 0.0, False, "no actionable liquidity event"


def _displacement_state(open_: np.ndarray, high: np.ndarray, low: np.ndarray, close: np.ndarray, atr: float) -> tuple[float, bool, str]:
    if len(close) == 0 or atr <= 0:
        return 0.0, False, "insufficient displacement history"
    rng = high[-1] - low[-1]
    body = abs(close[-1] - open_[-1])
    strong = rng >= atr * 1.25 and body / max(rng, np.finfo(float).eps) >= 0.65
    if not strong:
        return 0.0, False, "no qualifying displacement"
    return (0.8 if close[-1] > open_[-1] else -0.8), True, "high-range directional displacement"


def _premium_discount(high: np.ndarray, low: np.ndarray, close: np.ndarray) -> tuple[float, bool, str]:
    lookback = min(80, len(close))
    hi = float(np.max(high[-lookback:]))
    lo = float(np.min(low[-lookback:]))
    span = hi - lo
    if span <= 0:
        return 0.0, False, "invalid premium/discount range"
    eq = (hi + lo) / 2
    if close[-1] < eq - span * 0.02:
        return 0.45, True, "price in discount"
    if close[-1] > eq + span * 0.02:
        return -0.45, True, "price in premium"
    return 0.0, True, "price near equilibrium"


def _regime(adx: float | None, atr: float, close: float) -> str:
    if adx is None or not isfinite(adx) or atr <= 0 or close <= 0:
        return "insufficient"
    if adx >= 25:
        return "trending"
    if atr / close >= 0.006:
        return "volatile"
    if adx < 18:
        return "ranging"
    return "mixed"


def analyze(request: AnalysisRequest, as_of: str) -> UnifiedAnalysisRead:
    candles = request.candles[:-1] if request.closed_bar_only and len(request.candles) > 5 else request.candles
    if len(candles) < 5:
        raise ValueError("insufficient_closed_bars")
    opens, highs, lows, closes, volumes = _arr(request)
    if request.closed_bar_only and len(closes) > 5:
        opens, highs, lows, closes, volumes = (
            x[:-1] for x in (opens, highs, lows, closes, volumes)
        )

    ema20 = _last(talib.EMA(closes, timeperiod=20))
    ema50 = _last(talib.EMA(closes, timeperiod=50))
    ema200 = _last(talib.EMA(closes, timeperiod=200))
    rsi = _last(talib.RSI(closes, timeperiod=14))
    atr = _last(talib.ATR(highs, lows, closes, timeperiod=14)) or 0.0
    adx = _last(talib.ADX(highs, lows, closes, timeperiod=14))
    plus_di = _last(talib.PLUS_DI(highs, lows, closes, timeperiod=14)) or 0.0
    minus_di = _last(talib.MINUS_DI(highs, lows, closes, timeperiod=14)) or 0.0
    macd, signal, hist = talib.MACD(closes, fastperiod=12, slowperiod=26, signalperiod=9)
    macd_hist = _last(hist)
    bb_upper, bb_mid, bb_lower = talib.BBANDS(
        closes, timeperiod=20, nbdevup=2, nbdevdn=2, matype=0
    )
    close = float(closes[-1])

    trend_score = 0.0
    trend_facts: list[str] = []
    if ema20 is not None and ema50 is not None:
        trend_score += 0.35 if ema20 > ema50 else -0.35
        trend_facts.append("EMA20 above EMA50" if ema20 > ema50 else "EMA20 below EMA50")
    if ema50 is not None and ema200 is not None:
        trend_score += 0.35 if ema50 > ema200 else -0.35
        trend_facts.append("EMA50 above EMA200" if ema50 > ema200 else "EMA50 below EMA200")
    if plus_di > minus_di:
        trend_score += 0.2
    elif minus_di > plus_di:
        trend_score -= 0.2
    trend = _Module("trend", trend_score, 0.86 if adx and adx >= 20 else 0.65, "EMA and directional-index trend context", tuple(trend_facts))

    momentum_score = 0.0
    momentum_facts: list[str] = []
    if rsi is not None:
        momentum_score += max(-0.35, min(0.35, (rsi - 50) / 40))
        momentum_facts.append(f"RSI14={rsi:.1f}")
    if macd_hist is not None:
        momentum_score += 0.35 if macd_hist > 0 else -0.35
        momentum_facts.append("MACD histogram positive" if macd_hist > 0 else "MACD histogram negative")
    momentum = _Module("momentum", momentum_score, 0.8, "RSI and MACD momentum context", tuple(momentum_facts))

    structure_score, structure_confidence, structure_regime = _swing_state(highs, lows, closes)
    structure = _Module("structure", structure_score, structure_confidence, "confirmed swing structure", (structure_regime,))

    fvg_score, fvg_present, fvg_detail = _fvg_state(highs, lows)
    fvg_module = _Module("fvg", fvg_score, 0.7 if fvg_present else 0.35, fvg_detail, ())

    liquidity_score, liquidity_present, liquidity_detail = _liquidity_state(highs, lows, closes, atr)
    liquidity = _Module("liquidity", liquidity_score, 0.78 if liquidity_present else 0.35, liquidity_detail, ())

    displacement_score, displacement_present, displacement_detail = _displacement_state(opens, highs, lows, closes, atr)
    displacement = _Module("displacement", displacement_score, 0.82 if displacement_present else 0.35, displacement_detail, ())

    pd_score, pd_present, pd_detail = _premium_discount(highs, lows, closes)
    premium_discount = _Module("premium_discount", pd_score, 0.65 if pd_present else 0.3, pd_detail, ())

    modules = [trend, momentum, structure, fvg_module, liquidity, displacement, premium_discount]
    weighted = sum(m.score * m.confidence for m in modules)
    confidence_weight = sum(m.confidence for m in modules) or 1.0
    score = max(-1.0, min(1.0, weighted / confidence_weight))

    bullish_mtf = int(ema20 is not None and ema50 is not None and ema20 > ema50)
    bearish_mtf = int(ema20 is not None and ema50 is not None and ema20 < ema50)
    gates = [
        ConfluenceGate(
            id="htf_alignment",
            passed=bullish_mtf > 0 or bearish_mtf > 0,
            detail="directional EMA alignment is available",
        ),
        ConfluenceGate(
            id="liquidity_or_fvg",
            passed=liquidity_present or fvg_present,
            detail="liquidity event or FVG context is present",
        ),
        ConfluenceGate(
            id="zone_or_premium",
            passed=fvg_present or pd_present,
            detail="zone or premium/discount context is present",
        ),
        ConfluenceGate(
            id="displacement_or_structure",
            passed=displacement_present or abs(structure_score) >= 0.5,
            detail="displacement or confirmed structure impulse is present",
        ),
    ]
    passed = sum(g.passed for g in gates)
    confluence_score = int(round((passed / len(gates)) * 70 + min(30, abs(score) * 30)))
    accepted = (
        confluence_score >= request.min_confluence_score
        and passed >= 3
        and abs(score) >= 0.15
    )
    bias = _bias(score)
    recommendation = "long" if accepted and bias == "bullish" else "short" if accepted and bias == "bearish" else "wait"

    facts = [m.summary for m in modules if m.summary]
    risk = RiskTargetPlan(
        available=False,
        reason="account and broker risk context is required before sizing or executable targets",
    )
    regime = _regime(adx, atr, close)
    if regime == "insufficient" and structure_regime != "insufficient":
        regime = structure_regime

    return UnifiedAnalysisRead(
        symbol=request.symbol,
        timeframe=request.timeframe,
        as_of=as_of,
        bias=bias,
        score=score,
        confidence=min(1.0, confidence_weight / len(modules)),
        regime=regime,  # type: ignore[arg-type]
        recommendation=recommendation,
        modules=[_module_evidence(m) for m in modules],
        confluence_score=confluence_score,
        confluence_threshold=request.min_confluence_score,
        confluence_accepted=accepted,
        gates=gates,
        evidence=facts,
        risk_target=risk,
        closed_bar_time=candles[-1].time,
    )
