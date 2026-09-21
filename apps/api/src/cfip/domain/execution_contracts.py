from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

OrderSide = Literal["buy","sell"]
OrderType = Literal["market","limit","stop","stop-limit"]
ExecutionMode = Literal["paper","simulation","live"]

class OrderRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    client_order_id: str = Field(min_length=1, max_length=128)
    provider_id: str
    symbol: str
    side: OrderSide
    order_type: OrderType
    quantity: float = Field(gt=0)
    price: float | None = Field(default=None, gt=0)
    stop_loss: float | None = Field(default=None, gt=0)
    take_profit: float | None = Field(default=None, gt=0)
    mode: ExecutionMode = "paper"
    correlation_id: str = Field(min_length=1, max_length=128)

class ExecutionResult(BaseModel):
    model_config = ConfigDict(extra="forbid")
    client_order_id: str
    accepted: bool
    provider_order_id: str | None = None
    status: Literal["pending","accepted","rejected","filled","cancelled"] = "pending"
    reason: str | None = None
