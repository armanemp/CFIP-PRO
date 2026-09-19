"""Risk and position-sizing contracts; calculations remain replaceable services."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field
class PositionSizingRequest(BaseModel):
    model_config=ConfigDict(extra="forbid")
    equity:float=Field(gt=0); risk_fraction:float=Field(gt=0,le=1); entry_price:float=Field(gt=0)
    stop_price:float=Field(gt=0); contract_multiplier:float=Field(default=1,gt=0)
    leverage:float|None=Field(default=None,gt=0); direction:Literal["long","short"]
class PositionSizingResult(BaseModel):
    model_config=ConfigDict(extra="forbid")
    risk_amount:float=Field(ge=0); stop_distance:float=Field(gt=0); units:float=Field(gt=0)
    notional:float=Field(gt=0); leverage_used:float|None=Field(default=None,gt=0); warnings:tuple[str,...]=()
