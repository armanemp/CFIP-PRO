"""Safe admin control-plane runtime introspection endpoints."""

from fastapi import APIRouter

from cfip.core.config import get_settings
from cfip.infrastructure.runtime import platform_runtime

router = APIRouter(prefix="/admin", tags=["admin"])


@router.get("/runtime")
async def runtime() -> dict[str, object]:
    settings = get_settings()
    return {
        "app": {
            "name": settings.app_name,
            "version": settings.app_version,
            "environment": settings.app_env,
        },
        "endpoints": {"api_host": settings.api_host, "api_port": settings.api_port},
        "dependencies": {
            "postgres": bool(settings.database_url),
            "nats": bool(settings.nats_url),
            "redis": bool(settings.redis_url),
        },
        "platform": platform_runtime.snapshot(),
        "security": {
            "secrets_exposed": False,
            "mutation_enabled": False,
            "note": "Administrative mutations require the authenticated control-plane boundary.",
        },
    }
