"""Provider catalog endpoint.

This endpoint exposes configuration metadata only. Credentials and connection state are
handled by provider adapters and never returned by the catalog.
"""
from fastapi import APIRouter
from cfip.domain.provider_contracts import PROVIDER_CATALOG
from cfip.domain.provider_health import FailoverPolicy, ProviderHealth, eligible_providers

router = APIRouter(prefix="/providers", tags=["providers"])

@router.get("")
async def providers() -> list[dict[str, object]]:
    return [item.model_dump() for item in PROVIDER_CATALOG]


@router.post("/failover/eligible")
async def failover_eligible(
    policy: FailoverPolicy,
    health: list[ProviderHealth],
) -> dict[str, object]:
    selected = eligible_providers(tuple(health), policy)
    return {"providers": list(selected), "count": len(selected)}
