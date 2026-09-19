"""Composable market-analysis contracts.

Each analysis module emits normalized evidence; consensus is downstream and never owns
provider-specific calculations.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field
AnalysisKind=Literal["trend","structure","fvg","order-block","liquidity","momentum","volatility","mtf","confluence"]
class AnalysisEvidence(BaseModel):
    model_config=ConfigDict(extra="forbid")
    id:str=Field(min_length=1,max_length=128)
    kind:AnalysisKind
    symbol:str
    timeframe:str
    direction:Literal["bullish","bearish","neutral","mixed"]
    confidence:float=Field(ge=0,le=1)
    score:float|None=None
    rationale:str=Field(min_length=1,max_length=4000)
    source_module:str=Field(min_length=1,max_length=120)
    observed_at:int=Field(gt=0)
    valid_until:int|None=Field(default=None,gt=0)
    provenance:tuple[str,...]=()
class ConsensusResult(BaseModel):
    model_config=ConfigDict(extra="forbid")
    symbol:str
    timeframe:str
    direction:Literal["bullish","bearish","neutral","mixed"]
    confidence:float=Field(ge=0,le=1)
    evidence_ids:tuple[str,...]=()
    generated_at:int=Field(gt=0)
    calibration_version:str="1"
