"""Framework-independent market domain contracts."""

from datetime import UTC, datetime
from decimal import Decimal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class InstrumentCreate(BaseModel):
    symbol: str = Field(min_length=1, max_length=64)
    asset_class: str = Field(min_length=1, max_length=32)
    venue: str = Field(min_length=1, max_length=64)
    base_currency: str | None = Field(default=None, min_length=3, max_length=16)
    quote_currency: str | None = Field(default=None, min_length=3, max_length=16)


class InstrumentRead(InstrumentCreate):
    model_config = ConfigDict(from_attributes=True)

    id: UUID
    is_active: bool
    created_at: datetime


class MarketObservationCreate(BaseModel):
    instrument_id: UUID
    observed_at: datetime = Field(default_factory=lambda: datetime.now(UTC))
    bid: Decimal | None = Field(default=None, ge=0)
    ask: Decimal | None = Field(default=None, ge=0)
    last: Decimal | None = Field(default=None, ge=0)
    volume: Decimal | None = Field(default=None, ge=0)
    source: str = Field(min_length=1, max_length=64)
    source_event_id: str | None = Field(default=None, max_length=128)


class MarketObservationRead(MarketObservationCreate):
    model_config = ConfigDict(from_attributes=True)

    id: UUID
    created_at: datetime


class MarketObservationQuery(BaseModel):
    symbol: str = Field(min_length=1, max_length=64)
    venue: str = Field(min_length=1, max_length=64)
    start: datetime | None = None
    end: datetime | None = None
    limit: int = Field(default=500, ge=1, le=5000)
