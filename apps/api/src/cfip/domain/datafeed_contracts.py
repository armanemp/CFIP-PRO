"""Canonical market-data contracts shared by providers, cache, analysis and terminal."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

DataQualityStatus = Literal["verified", "degraded", "stale", "insufficient"]
MarketEventKind = Literal["tick", "bar", "quote", "depth", "trade"]

class InstrumentRef(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str = Field(min_length=1, max_length=64)
    venue: str = Field(min_length=1, max_length=80)
    asset_class: Literal["forex", "crypto", "equity", "future", "index", "other"] = "forex"

class DataProvenance(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str
    provider_event_id: str | None = None
    received_at: int = Field(gt=0)
    source_timestamp: int = Field(gt=0)
    quality: DataQualityStatus = "verified"
    revision: int = Field(default=0, ge=0)

class OHLCVBar(BaseModel):
    model_config = ConfigDict(extra="forbid")
    time: int = Field(gt=0)
    open: float
    high: float
    low: float
    close: float
    volume: float = Field(ge=0)
    provenance: DataProvenance

class Quote(BaseModel):
    model_config = ConfigDict(extra="forbid")
    time: int = Field(gt=0)
    bid: float | None = None
    ask: float | None = None
    last: float | None = None
    provenance: DataProvenance

class MarketDataRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    instrument: InstrumentRef
    timeframe: str
    start_time: int | None = Field(default=None, gt=0)
    end_time: int | None = Field(default=None, gt=0)
    limit: int = Field(default=500, ge=1, le=10000)
    provider_id: str | None = None


class MarketDataPage(BaseModel):
    model_config = ConfigDict(extra="forbid")
    request: MarketDataRequest
    bars: tuple[OHLCVBar, ...] = ()
    next_cursor: str | None = None

class ProviderHealth(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str
    status: Literal["healthy","degraded","down","unknown"] = "unknown"
    latency_ms: float | None = Field(default=None, ge=0)
    last_success_at: int | None = Field(default=None, gt=0)
    freshness_seconds: float | None = Field(default=None, ge=0)
    message: str = ""

class DataFeedCapabilities(BaseModel):
    model_config = ConfigDict(extra="forbid")
    historical: bool = False
    realtime: bool = False
    ticks: bool = False
    quotes: bool = False
    depth: bool = False
    trades: bool = False
