"""Single typed inventory of configurable platform domains."""
from pydantic import BaseModel, ConfigDict, Field
from cfip.domain.config_contracts import (
    ChartConfig, GitGovernanceConfig, IntelligenceConfig, NotificationConfig,
    ProviderConfig, RiskConfig,
)
from cfip.domain.provider_contracts import PROVIDER_CATALOG

def default_provider_configs() -> tuple[ProviderConfig, ...]:
    """Expose every catalog provider in settings without implying connectivity."""
    return tuple(
        ProviderConfig(
            provider_id=descriptor.id,
            enabled=False,
            priority=100,
            settings={"catalog_status": descriptor.status},
        )
        for descriptor in PROVIDER_CATALOG
    )

class PlatformSettings(BaseModel):
    model_config = ConfigDict(extra="forbid")
    chart: ChartConfig = Field(default_factory=ChartConfig)
    intelligence: IntelligenceConfig = Field(default_factory=IntelligenceConfig)
    notifications: NotificationConfig = Field(default_factory=NotificationConfig)
    risk: RiskConfig = Field(default_factory=RiskConfig)
    git: GitGovernanceConfig = Field(default_factory=GitGovernanceConfig)
    providers: tuple[ProviderConfig, ...] = Field(default_factory=default_provider_configs)
    enabled_modules: tuple[str, ...] = (
        "market-data","indicators","analysis","intelligence","risk","alerts",
        "workspaces","research","paper-trading","execution",
    )
