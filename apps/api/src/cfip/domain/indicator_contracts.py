"""Provider-neutral indicator contracts and lifecycle boundaries."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field
IndicatorKind = Literal["overlay","oscillator","volume","volatility","trend","momentum","structure","custom"]
class IndicatorDefinition(BaseModel):
    model_config=ConfigDict(extra="forbid")
    id:str=Field(min_length=1,max_length=80)
    name:str=Field(min_length=1,max_length=120)
    kind:IndicatorKind
    parameters:dict[str,float|int|str|bool]= {}
    output_names:tuple[str,...]=()
    source:str="cfip"
    version:str="1"
class IndicatorInstance(BaseModel):
    model_config=ConfigDict(extra="forbid")
    instance_id:str=Field(min_length=1,max_length=128)
    definition_id:str
    symbol:str
    timeframe:str
    parameters:dict[str,float|int|str|bool]={}
    visible:bool=True
    pane:str="chart"
class IndicatorPoint(BaseModel):
    model_config=ConfigDict(extra="forbid")
    time:int=Field(gt=0)
    values:dict[str,float|None]
