"""Governed intelligence change lifecycle."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field
ChangeStage=Literal["proposed","validated","approved","applied","verified","rolled-back","rejected"]
ChangeRisk=Literal["low","medium","high","critical"]
class ChangeEvidence(BaseModel):
    model_config=ConfigDict(extra="forbid")
    id:str=Field(min_length=1,max_length=128); kind:str=Field(min_length=1,max_length=80)
    summary:str=Field(min_length=1,max_length=2000); artifact_refs:tuple[str,...]=()
class GovernedChange(BaseModel):
    model_config=ConfigDict(extra="forbid")
    id:str=Field(min_length=1,max_length=128); title:str=Field(min_length=1,max_length=200)
    stage:ChangeStage="proposed"; risk:ChangeRisk="medium"
    requested_by:str=Field(min_length=1,max_length=120); target_paths:tuple[str,...]=()
    validation_plan:tuple[str,...]=(); rollback_plan:tuple[str,...]=()
    evidence:tuple[ChangeEvidence,...]=(); human_approval_required:bool=True; git_branch:str|None=None
