"""Read-only effective configuration metadata plus dynamic identity."""
from fastapi import APIRouter
from cfip.core.config import get_settings
from cfip.domain.config_contracts import ChartConfig, GitGovernanceConfig, IntelligenceConfig, NotificationConfig, RiskConfig
from cfip.infrastructure.runtime_identity import platform_identity

router = APIRouter(prefix="/config", tags=["config"])

@router.get("/defaults")
async def defaults() -> dict[str, object]:
    identity = await platform_identity.get_async()
    intelligence = IntelligenceConfig(identity=identity)
    return {
        "risk": RiskConfig().model_dump(),
        "intelligence": intelligence.model_dump(),
        "intelligence_identity": identity.model_dump(),
        "chart": ChartConfig().model_dump(),
        "notifications": NotificationConfig().model_dump(),
        "git_governance": GitGovernanceConfig().model_dump(),
        "mutation_enabled": bool(get_settings().admin_control_token),
        "secrets_exposed": False,
    }
