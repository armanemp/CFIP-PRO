"""Read-only effective configuration metadata.

Mutation remains behind authenticated RBAC, persistence and audit. The endpoint exposes
typed defaults and capability policy without secrets.
"""
from fastapi import APIRouter
from cfip.domain.platform_settings import PlatformSettings

router = APIRouter(prefix="/config", tags=["config"])

@router.get("/defaults")
async def defaults() -> dict[str, object]:
    settings = PlatformSettings()
    return {
        "settings": settings.model_dump(),
        "mutation_enabled": False,
        "secrets_exposed": False,
    }
