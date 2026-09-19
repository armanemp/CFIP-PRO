"""Single typed inventory of configurable platform domains."""
from pydantic import BaseModel, ConfigDict, Field
from cfip.domain.config_contracts import ChartConfig, GitGovernanceConfig, IntelligenceConfig, NotificationConfig, RiskConfig, ProviderConfig

class PlatformSettings(BaseModel):
    model_config = ConfigDict(extra="forbid")
    chart: ChartConfig = Field(default_factory=ChartConfig)
    intelligence: IntelligenceConfig = Field(default_factory=IntelligenceConfig)
    notifications: NotificationConfig = Field(default_factory=NotificationConfig)
    risk: RiskConfig = Field(default_factory=RiskConfig)
    git: GitGovernanceConfig = Field(default_factory=GitGovernanceConfig)
    providers: tuple[ProviderConfig, ...] = ()
    enabled_modules: tuple[str, ...] = (
        "market-data","indicators","analysis","intelligence","risk","alerts",
        "workspaces","research","paper-trading","execution",
    )
