"""Shared control-plane contracts for settings, proposals and verification."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

ControlAction = Literal["inspect", "propose", "validate", "approve", "promote", "rollback"]

class ControlRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    action: ControlAction
    actor: str = Field(min_length=1, max_length=200)
    correlation_id: str = Field(min_length=1, max_length=128)
    target: str = Field(min_length=1, max_length=300)
    reason: str = Field(default="", max_length=2000)

class VerificationResult(BaseModel):
    model_config = ConfigDict(extra="forbid")
    run_id: str
    passed: bool
    checks: tuple[str, ...]
    failures: tuple[str, ...] = ()
    rollback_available: bool = True
