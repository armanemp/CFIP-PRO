"""Canonical, provider-neutral risk and position-sizing contracts."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

RiskSide = Literal["long", "short"]


class SymbolTradingRules(BaseModel):
    model_config = ConfigDict(extra="forbid")

    symbol: str = Field(min_length=1, max_length=32)
    tick_size: float = Field(gt=0)
    tick_value: float = Field(gt=0)
    contract_size: float = Field(gt=0)
    min_quantity: float = Field(gt=0)
    max_quantity: float = Field(gt=0)
    quantity_step: float = Field(gt=0)


class PositionSizingRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    equity: float = Field(gt=0)
    risk_percent: float = Field(gt=0, le=100)
    entry: float = Field(gt=0)
    stop_loss: float = Field(gt=0)
    rules: SymbolTradingRules
    account_currency: str = Field(min_length=3, max_length=12)
    quote_currency: str = Field(min_length=3, max_length=12)


class RiskRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    symbol: str = Field(min_length=1, max_length=32)
    side: RiskSide
    equity: float = Field(gt=0)
    risk_fraction: float = Field(gt=0, le=1)
    entry_price: float = Field(gt=0)
    stop_price: float = Field(gt=0)
    take_profit_price: float | None = Field(default=None, gt=0)
    leverage: float = Field(gt=0)
    contract_size: float = Field(default=1, gt=0)
    quote_to_account_rate: float = Field(default=1, gt=0)

    @model_validator(mode="after")
    def validate_direction(self) -> "RiskRequest":
        if self.side == "long" and self.stop_price >= self.entry_price:
            raise ValueError("long_stop_must_be_below_entry")
        if self.side == "short" and self.stop_price <= self.entry_price:
            raise ValueError("short_stop_must_be_above_entry")
        if self.take_profit_price is not None:
            if self.side == "long" and self.take_profit_price <= self.entry_price:
                raise ValueError("long_take_profit_must_exceed_entry")
            if self.side == "short" and self.take_profit_price >= self.entry_price:
                raise ValueError("short_take_profit_must_be_below_entry")
        return self


class RiskCalculation(BaseModel):
    model_config = ConfigDict(extra="forbid")

    symbol: str
    side: RiskSide
    risk_amount: float = Field(ge=0)
    stop_distance: float = Field(gt=0)
    stop_distance_fraction: float = Field(gt=0)
    units_by_risk: float = Field(ge=0)
    units_by_leverage: float = Field(ge=0)
    recommended_units: float = Field(ge=0)
    notional: float = Field(ge=0)
    margin_required: float = Field(ge=0)
    risk_fraction: float = Field(gt=0, le=1)
    warnings: list[str] = Field(default_factory=list, max_length=20)


class RiskLimit(BaseModel):
    model_config = ConfigDict(extra="forbid")

    name: str = Field(min_length=1, max_length=64)
    maximum_fraction: float = Field(gt=0, le=1)
    enabled: bool = True
    scope: Literal["trade", "symbol", "portfolio", "session"]


class RiskDecision(BaseModel):
    model_config = ConfigDict(extra="forbid")

    decision: Literal["accepted", "rejected"]
    quantity: float = Field(ge=0)
    risk_amount: float = Field(ge=0)
    reason: str | None = None
