"""Read-only platform capability registry endpoint."""
from fastapi import APIRouter
from cfip.domain.platform_capabilities import capabilities
from cfip.infrastructure.providers.oss_registry import capability_snapshot

router = APIRouter(prefix="/capabilities", tags=["capabilities"])

@router.get("")
async def list_capabilities() -> list[dict[str, object]]:
    return [item.model_dump() for item in capabilities()] + [{"kind": "oss", **item} for item in capability_snapshot()]
