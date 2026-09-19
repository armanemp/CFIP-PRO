"""Read-only effective configuration metadata.

The endpoint exposes typed defaults for the control plane. Mutation remains behind
authenticated RBAC, persistence, validation and audit boundaries.
"""

from fastapi import APIRouter

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
    return {
        "risk": RiskConfig().model_dump(),
        "intelligence": IntelligenceConfig().model_dump(),
        "chart": ChartConfig().model_dump(),
        "notifications": NotificationConfig().model_dump(),
        "git_governance": GitGovernanceConfig().model_dump(),
        "mutation_enabled": False,
        "secrets_exposed": False,
    }
