"""Canonical backend analysis contracts.

The backend owns the normalized analysis envelope consumed by the terminal, replay,
alerts and future AI orchestration. Provider-specific implementations never cross
this boundary.
"""

from math import isfinite
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

from cfip.domain.data_quality import DataQualityReport

Bias = Literal["bullish", "bearish", "neutral"]
Recommendation = Literal["long", "short", "wait"]
Regime = Literal["trending", "ranging", "volatile", "mixed", "insufficient"]


class CandleInput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    time: int = Field(gt=0)
    open: float = Field(gt=0)
    high: float = Field(gt=0)
    low: float = Field(gt=0)
    close: float = Field(gt=0)
    volume: float = Field(default=0, ge=0)

    @model_validator(mode="before")
    @classmethod
    def validate_raw_ohlc(cls, data: object) -> object:
        if not isinstance(data, dict):
            return data
        values = [data.get(name) for name in ("open", "high", "low", "close", "volume")]
        if any(value is not None and isinstance(value, (int, float)) and not isfinite(value) for value in values):
            raise ValueError("non_finite_ohlc")
        prices = [data.get(name) for name in ("open", "high", "low", "close")]
        if any(value is not None and isinstance(value, (int, float)) and value <= 0 for value in prices):
            raise ValueError("invalid_ohlc")
        return data

    @model_validator(mode="after")
    def validate_ohlc(self) -> "CandleInput":
        if not all(isfinite(value) for value in (self.open, self.high, self.low, self.close, self.volume)):
            raise ValueError("non_finite_ohlc")
        if self.high < max(self.open, self.close) or self.low > min(self.open, self.close):
            raise ValueError("invalid_ohlc")
        if self.high < self.low:
            raise ValueError("invalid_ohlc_range")
        return self


class AnalysisRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    symbol: str = Field(min_length=1, max_length=64)
    timeframe: str = Field(min_length=1, max_length=16)
    candles: list[CandleInput] = Field(min_length=5, max_length=20000)
    min_confluence_score: int = Field(default=84, ge=0, le=100)
    minimum_aligned_htfs: int = Field(default=2, ge=0, le=5)
    closed_bar_only: bool = True

    @model_validator(mode="after")
    def validate_candle_order(self) -> "AnalysisRequest":
        if any(current.time <= previous.time for previous, current in zip(self.candles, self.candles[1:], strict=False)):
            raise ValueError("candles_must_be_strictly_increasing")
        return self


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


class FVGState(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str
    direction: Literal["bullish", "bearish"]
    lower: float = Field(gt=0)
    upper: float = Field(gt=0)
    state: Literal["active", "partial", "mitigated", "invalidated"]
    origin_time: int = Field(gt=0)
    last_evaluated_time: int = Field(gt=0)
    mitigation_ratio: float = Field(ge=0, le=1)


class OrderBlockState(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str
    direction: Literal["bullish", "bearish"]
    low: float = Field(gt=0)
    high: float = Field(gt=0)
    state: Literal["active", "mitigated", "invalidated", "breaker"]
    origin_time: int = Field(gt=0)
    last_evaluated_time: int = Field(gt=0)
    displacement_time: int | None = None
    structure_break_time: int | None = None


class LiquidityPool(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str
    side: Literal["buy_side", "sell_side"]
    price: float = Field(gt=0)
    strength: int = Field(ge=1, le=100)
    swept: bool
    origin_time: int = Field(gt=0)


class MTFContext(BaseModel):
    model_config = ConfigDict(extra="forbid")

    timeframe: str
    closed_bar_time: int
    bias: Bias
    score: float = Field(ge=-1, le=1)
    confidence: float = Field(ge=0, le=1)
    candle_count: int = Field(ge=0)
    completeness: float = Field(ge=0, le=1)


class RiskTargetPlan(BaseModel):
    model_config = ConfigDict(extra="forbid")

    available: bool = False
    reason: str | None = None
    entry: float | None = None
    stop: float | None = None
    risk_distance: float | None = None
    tp1: float | None = None
    tp2: float | None = None
    tp3: float | None = None
    rr1: float | None = None
    rr2: float | None = None
    rr3: float | None = None


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
    fvg_states: list[FVGState] = Field(default_factory=list)
    order_blocks: list[OrderBlockState] = Field(default_factory=list)
    liquidity_pools: list[LiquidityPool] = Field(default_factory=list)
    mtf_contexts: list[MTFContext] = Field(default_factory=list)
    risk_target: RiskTargetPlan
    closed_bar_time: int | None = None
    data_quality: DataQualityReport
