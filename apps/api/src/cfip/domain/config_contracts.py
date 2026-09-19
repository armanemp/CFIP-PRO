"""Provider-neutral configuration contracts.

Values are validated at the boundary. Secrets are represented by references rather than
raw values, and mutable configuration is expected to be persisted/audited by the control plane.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

class SecretRef(BaseModel):
    model_config = ConfigDict(extra="forbid")
    ref: str = Field(min_length=1, max_length=256)

class ProviderConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str = Field(min_length=1, max_length=80)
    enabled: bool = True
    priority: int = Field(default=100, ge=0, le=1000)
    secret_refs: tuple[SecretRef, ...] = ()
    settings: dict[str, str | int | float | bool] = {}

class RiskConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    max_account_risk_percent: float = Field(default=1.0, ge=0, le=100)
    max_open_positions: int = Field(default=10, ge=0, le=10000)
    kill_switch: bool = False

class IntelligenceConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str = "deterministic"
    require_evidence: bool = True
    min_confidence: float = Field(default=0.0, ge=0, le=1)
    auto_promotion: bool = False
    learning_enabled: bool = True
