"""Read-only effective configuration metadata.

The endpoint exposes typed defaults for the control plane. Mutation remains behind
authenticated RBAC, persistence, validation and audit boundaries.
"""

from fastapi import APIRouter

from cfip.core.config import get_settings
from cfip.domain.config_contracts import (
    ChartConfig,
    GitGovernanceConfig,
    IntelligenceConfig,
    NotificationConfig,
    RiskConfig,
)

router = APIRouter(prefix="/config", tags=["config"])


@router.get("/defaults")
async def defaults() -> dict[str, object]:
    settings = get_settings()
    intelligence = IntelligenceConfig(
        identity=IntelligenceConfig().identity.model_copy(update={"name": settings.intelligence_name})
    )
    return {
        "risk": RiskConfig().model_dump(),
        "intelligence": intelligence.model_dump(),
        "intelligence_identity": intelligence.identity.model_dump(),
        "chart": ChartConfig().model_dump(),
        "notifications": NotificationConfig().model_dump(),
        "git_governance": GitGovernanceConfig().model_dump(),
        "mutation_enabled": False,
        "secrets_exposed": False,
    }
