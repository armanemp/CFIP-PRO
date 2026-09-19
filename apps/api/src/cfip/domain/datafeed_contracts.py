"""Historical and realtime datafeed contracts.

Adapters may speak REST, websocket, FIX, broker APIs or vendor SDKs. The terminal never
depends on those transports directly; all feeds normalize through these contracts.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

FeedKind = Literal["historical", "realtime"]
BarStatus = Literal["open", "closed"]

class DatafeedRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str = Field(min_length=1, max_length=64)
    venue: str = Field(default="reference", min_length=1, max_length=64)
    timeframe: str = Field(min_length=1, max_length=16)
    start_time: int | None = Field(default=None, gt=0)
    end_time: int | None = Field(default=None, gt=0)
    limit: int = Field(default=500, ge=1, le=100000)
    feed: FeedKind = "historical"

class NormalizedBar(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str
    venue: str
    timeframe: str
    time: int = Field(gt=0)
    open: float = Field(gt=0)
    high: float = Field(gt=0)
    low: float = Field(gt=0)
    close: float = Field(gt=0)
    volume: float = Field(default=0, ge=0)
    status: BarStatus = "closed"
    source: str = Field(min_length=1, max_length=80)
    sequence: int | None = Field(default=None, ge=0)
    quality: Literal["verified", "delayed", "stale", "degraded", "unknown"] = "unknown"

class FeedHealth(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str
    connected: bool
    latency_ms: float | None = Field(default=None, ge=0)
    last_event_at: int | None = Field(default=None, gt=0)
    error: str | None = None

class DatafeedSnapshot(BaseModel):
    model_config = ConfigDict(extra="forbid")
    request: DatafeedRequest
    bars: tuple[NormalizedBar, ...] = ()
    health: FeedHealth
