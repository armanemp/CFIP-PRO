from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

class SymbolTradingRules(BaseModel):
    model_config = ConfigDict(extra="forbid")
    symbol: str
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
    account_currency: str
    quote_currency: str

class RiskDecision(BaseModel):
    model_config = ConfigDict(extra="forbid")
    decision: Literal["accepted", "rejected"]
    quantity: float = Field(ge=0)
    risk_amount: float = Field(ge=0)
    reason: str | None = None
