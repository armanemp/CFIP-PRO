"""Cross-domain contracts shared by terminal, data, analysis and intelligence."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

Quality = Literal["verified","delayed","stale","degraded","unknown"]

class InstrumentRef(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str = Field(min_length=1, max_length=64)
    venue: str = Field(default="reference", min_length=1, max_length=64)

class MarketTick(BaseModel):
    model_config = ConfigDict(extra="forbid")
    instrument: InstrumentRef
    ts: int = Field(gt=0)
    bid: float | None = None
    ask: float | None = None
    last: float | None = None
    volume: float | None = None
    sequence: int | None = Field(default=None, ge=0)
    quality: Quality = "unknown"

class AnalysisRef(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str
    timeframe: str
    bias: Literal["bullish","bearish","neutral"]
    confidence: float = Field(ge=0, le=1)
    recommendation: Literal["long","short","wait"]
    evidence_ids: tuple[str, ...] = ()

class TerminalEvent(BaseModel):
    model_config = ConfigDict(extra="forbid")
    event_type: Literal["tick","bar","analysis","alert","notification","execution","risk"]
    occurred_at: int = Field(gt=0)
    correlation_id: str = Field(min_length=1, max_length=128)
    instrument: InstrumentRef | None = None
    payload: dict[str, object] = {}
