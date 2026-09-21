"""Provider-neutral configuration contracts.

Values are validated at the boundary. Secrets are represented by references rather than
raw values, and mutable configuration is expected to be persisted/audited by the control plane.
"""
from pydantic import BaseModel, ConfigDict, Field

from cfip.domain.intelligence_identity import DEFAULT_INTELLIGENCE_IDENTITY, IntelligenceIdentity


class SecretRef(BaseModel):
    model_config = ConfigDict(extra="forbid")
    ref: str = Field(min_length=1, max_length=256)


class ProviderConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    provider_id: str = Field(min_length=1, max_length=80)
    enabled: bool = True
    priority: int = Field(default=100, ge=0, le=1000)
    secret_refs: tuple[SecretRef, ...] = ()
    settings: dict[str, str | int | float | bool] = Field(default_factory=dict)


class RiskConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    max_account_risk_percent: float = Field(default=1.0, ge=0, le=100)
    max_open_positions: int = Field(default=10, ge=0, le=10000)
    kill_switch: bool = False


class IntelligenceConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    identity: IntelligenceIdentity = Field(default_factory=lambda: DEFAULT_INTELLIGENCE_IDENTITY.model_copy(deep=True))
    provider_id: str = "deterministic"
    require_evidence: bool = True
    min_confidence: float = Field(default=0.0, ge=0, le=1)
    auto_promotion: bool = False
    learning_enabled: bool = True


class ChartConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    default_timeframe: str = "1m"
    default_chart_type: str = "candles"
    max_visible_indicators: int = Field(default=12, ge=1, le=100)
    persist_workspace: bool = True
    enable_replay: bool = True


class NotificationConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    enabled: bool = True
    cooldown_seconds: int = Field(default=60, ge=0)
    max_active_rules: int = Field(default=100, ge=0, le=10000)


class GitGovernanceConfig(BaseModel):
    model_config = ConfigDict(extra="forbid")
    enabled: bool = True
    require_human_approval: bool = True
    allow_pull_requests: bool = True
    allow_direct_main_commit: bool = False
    protected_paths: tuple[str, ...] = (".github/workflows/", ".env", ".env.")
