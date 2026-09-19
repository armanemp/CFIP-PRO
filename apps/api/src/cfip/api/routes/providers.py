"""Provider catalog endpoint.

This endpoint exposes configuration metadata only. Credentials and connection state are
handled by provider adapters and never returned by the catalog.
"""
from fastapi import APIRouter
from cfip.domain.provider_contracts import PROVIDER_CATALOG

router = APIRouter(prefix="/providers", tags=["providers"])

@router.get("")
async def providers() -> list[dict[str, object]]:
    return [item.model_dump() for item in PROVIDER_CATALOG]
