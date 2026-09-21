"""Administrative runtime control-plane endpoints."""
from fastapi import APIRouter, Header, HTTPException
from pydantic import BaseModel, Field

from cfip.core.config import get_settings
from cfip.infrastructure.runtime import platform_runtime
from cfip.infrastructure.runtime_identity import platform_identity

router = APIRouter(prefix="/admin", tags=["admin"])

class IdentityUpdate(BaseModel):
    name: str = Field(min_length=2, max_length=48, pattern=r"^[A-Za-z0-9][A-Za-z0-9 ._-]*$")

def _require_admin_token(token: str | None) -> None:
    expected = get_settings().admin_control_token
    if not expected or token != expected:
        raise HTTPException(status_code=403, detail="admin_control_not_authorized")

@router.get("/runtime")
async def runtime() -> dict[str, object]:
    settings = get_settings()
    identity = await platform_identity.get_async()
    return {
        "app": {
            "name": identity.name,
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
            "mutation_enabled": bool(settings.admin_control_token),
            "authentication": "admin-control-token",
        },
    }

@router.get("/identity")
async def identity() -> dict[str, object]:
    return (await platform_identity.get_async()).model_dump()

@router.put("/identity")
async def update_identity(payload: IdentityUpdate, x_admin_control_token: str | None = Header(default=None)) -> dict[str, object]:
    _require_admin_token(x_admin_control_token)
    return (await platform_identity.set_name_async(payload.name)).model_dump()
