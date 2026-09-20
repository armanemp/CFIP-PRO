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
from cfip.domain.intelligence_identity import DEFAULT_INTELLIGENCE_IDENTITY

router = APIRouter(prefix="/config", tags=["config"])


@router.get("/defaults")
async def defaults() -> dict[str, object]:
    settings = get_settings()
    identity = DEFAULT_INTELLIGENCE_IDENTITY.model_copy(update={"name": settings.intelligence_name})
    return {
        "risk": RiskConfig().model_dump(),
        "intelligence": IntelligenceConfig().model_dump(),
        "intelligence_identity": identity.model_dump(),
        "chart": ChartConfig().model_dump(),
        "notifications": NotificationConfig().model_dump(),
        "git_governance": GitGovernanceConfig().model_dump(),
        "mutation_enabled": False,
        "secrets_exposed": False,
    }
