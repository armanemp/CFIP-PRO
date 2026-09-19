"""Replay/backtest contracts shared by live and historical execution."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field
ReplayMode=Literal["bar","tick","event"]
class ReplayRequest(BaseModel):
    model_config=ConfigDict(extra="forbid")
    symbol:str=Field(min_length=1,max_length=64)
    venue:str=Field(min_length=1,max_length=64)
    timeframe:str=Field(min_length=1,max_length=16)
    start_time:int=Field(gt=0)
    end_time:int=Field(gt=0)
    mode:ReplayMode="bar"
    speed:float=Field(default=1,gt=0,le=1000)
    seed:int|None=None
class ReplayCursor(BaseModel):
    model_config=ConfigDict(extra="forbid")
    request:ReplayRequest
    current_time:int=Field(gt=0)
    emitted_events:int=Field(default=0,ge=0)
    running:bool=False
