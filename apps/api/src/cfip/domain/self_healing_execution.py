"""Governed execution boundary for self-healing and self-development.

This module intentionally contains no shell/process execution. It defines the policy
and evidence required before an external executor may apply a repair.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

ExecutionStatus = Literal["blocked","ready","executed","failed","rolled_back"]
ArtifactKind = Literal["config","cache","index","service","code","dependency","documentation"]

class SafetyInvariant(BaseModel):
    model_config=ConfigDict(extra="forbid")
    id:str=Field(min_length=1,max_length=128)
    description:str=Field(min_length=1,max_length=1000)
    required:bool=True

class RepairExecutionRequest(BaseModel):
    model_config=ConfigDict(extra="forbid")
    proposal_id:str=Field(min_length=1,max_length=128)
    artifact_kind:ArtifactKind
    artifact_digest:str=Field(min_length=16,max_length=128)
    rollback_digest:str=Field(min_length=16,max_length=128)
    test_evidence_ids:list[str]=Field(min_length=1,max_length=100)
    safety_invariant_ids:list[str]=Field(min_length=1,max_length=100)
    canary_required:bool=True
    approval_id:str|None=None
    status:ExecutionStatus="blocked"

class SelfDevelopmentChange(BaseModel):
    model_config=ConfigDict(extra="forbid")
    change_id:str=Field(min_length=1,max_length=128)
    branch_ref:str=Field(min_length=1,max_length=256)
    artifact_digest:str=Field(min_length=16,max_length=128)
    test_evidence_ids:list[str]=Field(min_length=1,max_length=100)
    security_evidence_ids:list[str]=Field(default_factory=list,max_length=100)
    review_required:bool=True
    production_apply_allowed:bool=False

class ExecutionGateResult(BaseModel):
    model_config=ConfigDict(extra="forbid")
    allowed:bool
    reasons:list[str]=Field(default_factory=list,max_length=50)

class SelfHealingExecutionPolicy(BaseModel):
    model_config=ConfigDict(extra="forbid")
    require_approval_for_code:bool=True
    require_signed_artifact:bool=True
    require_rollback_digest:bool=True
    require_canary:bool=True
    min_test_evidence:int=1
    max_consecutive_repairs:int=3
    cooldown_seconds:int=300
    circuit_breaker_after_failures:int=3
