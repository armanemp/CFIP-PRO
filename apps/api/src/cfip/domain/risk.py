"""Canonical broker-aware risk and target contracts.

Risk calculations require explicit account and instrument metadata. Missing conversion,
pip/tick value, stop distance or broker constraints produce an unavailable result rather
than a fabricated position size.
"""

from math import isfinite
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

Direction = Literal["long", "short"]


class AccountRiskContext(BaseModel):
    model_config = ConfigDict(extra="forbid")

    equity: float = Field(gt=0)
    risk_fraction: float = Field(gt=0, le=1)
    leverage: float = Field(gt=0)
    account_currency: str = Field(min_length=3, max_length=16)

    @model_validator(mode="after")
    def validate_finite(self) -> "AccountRiskContext":
        if not all(isfinite(value) for value in (self.equity, self.risk_fraction, self.leverage)):
            raise ValueError("non_finite_account_risk")
        return self


class InstrumentRiskContext(BaseModel):
    model_config = ConfigDict(extra="forbid")

    symbol: str = Field(min_length=1, max_length=64)
    base_currency: str = Field(min_length=3, max_length=16)
    quote_currency: str = Field(min_length=3, max_length=16)
    pip_size: float = Field(gt=0)
    tick_size: float = Field(gt=0)
    tick_value_per_unit: float = Field(gt=0)
    min_stop_distance: float = Field(gt=0)
    min_quantity: float = Field(gt=0)
    max_quantity: float = Field(gt=0)
    quantity_step: float = Field(gt=0)

    @model_validator(mode="after")
    def validate_constraints(self) -> "InstrumentRiskContext":
        values = (
            self.pip_size,
            self.tick_size,
            self.tick_value_per_unit,
            self.min_stop_distance,
            self.min_quantity,
            self.max_quantity,
            self.quantity_step,
        )
        if not all(isfinite(value) for value in values):
            raise ValueError("non_finite_instrument_risk")
        if self.max_quantity < self.min_quantity:
            raise ValueError("max_quantity_below_minimum")
        return self


class RiskTargetRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    direction: Direction
    entry: float = Field(gt=0)
    atr: float = Field(gt=0)
    stop_atr_multiplier: float = Field(gt=0)
    target_rr: tuple[float, float, float] = Field(min_length=3, max_length=3)
    minimum_rr: float = Field(gt=0)

    @model_validator(mode="after")
    def validate_targets(self) -> "RiskTargetRequest":
        values = (self.entry, self.atr, self.stop_atr_multiplier, self.minimum_rr, *self.target_rr)
        if not all(isfinite(value) for value in values):
            raise ValueError("non_finite_risk_target")
        if any(rr <= 0 for rr in self.target_rr):
            raise ValueError("target_rr_must_be_positive")
        if any(rr < self.minimum_rr for rr in self.target_rr):
            raise ValueError("target_rr_below_minimum")
        return self


class RiskTargetPlan(BaseModel):
    model_config = ConfigDict(extra="forbid")

    available: bool
    reason: str
    direction: Direction | None = None
    entry: float | None = None
    stop: float | None = None
    tp1: float | None = None
    tp2: float | None = None
    tp3: float | None = None
    risk_distance: float | None = None
    risk_amount: float | None = None
    quantity: float | None = None
    margin_required: float | None = None

    @model_validator(mode="after")
    def validate_finite_outputs(self) -> "RiskTargetPlan":
        values = (
            self.entry,
            self.stop,
            self.tp1,
            self.tp2,
            self.tp3,
            self.risk_distance,
            self.risk_amount,
            self.quantity,
            self.margin_required,
        )
        if any(value is not None and not isfinite(value) for value in values):
            raise ValueError("non_finite_risk_output")
        return self
