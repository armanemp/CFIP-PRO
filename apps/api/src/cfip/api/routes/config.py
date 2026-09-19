"""Read-only effective configuration metadata.

Mutation is intentionally excluded until authenticated RBAC + persistence + audit are
connected. This prevents a UI control from becoming an unaudited production mutation.
"""
from fastapi import APIRouter
from cfip.domain.config_contracts import IntelligenceConfig, RiskConfig

router = APIRouter(prefix="/config", tags=["config"])

@router.get("/defaults")
async def defaults() -> dict[str, object]:
    return {
        "risk": RiskConfig().model_dump(),
        "intelligence": IntelligenceConfig().model_dump(),
        "mutation_enabled": False,
        "secrets_exposed": False,
    }
