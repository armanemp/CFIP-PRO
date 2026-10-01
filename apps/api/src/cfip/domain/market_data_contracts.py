"""Vendor-neutral historical market-data contracts."""
from __future__ import annotations
from typing import Protocol
from pydantic import BaseModel, ConfigDict, Field
from cfip.domain.analysis import CandleInput

class HistoricalMarketDataRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str = Field(min_length=1, max_length=64)
    timeframe: str = Field(min_length=1, max_length=16)
    start_epoch: int | None = Field(default=None, gt=0)
    end_epoch: int | None = Field(default=None, gt=0)
    limit: int = Field(default=500, ge=5, le=100_000)

    def model_post_init(self, __context: object) -> None:
        if self.start_epoch is not None and self.end_epoch is not None and self.end_epoch <= self.start_epoch:
            raise ValueError("end_epoch_must_follow_start_epoch")

class HistoricalMarketDataResult(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str
    timeframe: str
    candles: list[CandleInput]
    provider: str
    provider_version: str
    provenance: str
    fetched_at_epoch: int = Field(gt=0)

class MarketDataAdapter(Protocol):
    id: str
    version: str
    def available(self) -> bool: ...
    def fetch_historical(self, request: HistoricalMarketDataRequest) -> HistoricalMarketDataResult: ...
