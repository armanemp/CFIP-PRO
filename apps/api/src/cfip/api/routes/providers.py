"""Provider catalog and failover contract endpoints.

Catalog metadata is deliberately separate from live connectivity. An entry marked
adapter or verified describes implementation maturity, not the current credential
or session state of an individual deployment.
"""

from fastapi import APIRouter

from cfip.domain.provider_contracts import PROVIDER_CATALOG
from cfip.domain.provider_health import FailoverPolicy, ProviderHealth, eligible_providers

router = APIRouter(prefix="/providers", tags=["providers"])


@router.get("")
async def providers() -> list[dict[str, object]]:
    return [item.model_dump() for item in PROVIDER_CATALOG]


@router.get("/status")
async def provider_status() -> list[dict[str, object]]:
    return [
        {
            "provider_id": item.id,
            "catalog_status": item.status,
            "connection_state": "unknown",
            "credential_required": item.credential_required,
            "capabilities": list(item.capabilities),
            "note": "Live connectivity is reported only by a configured provider adapter.",
        }
        for item in PROVIDER_CATALOG
    ]


@router.post("/failover/eligible")
async def failover_eligible(
    policy: FailoverPolicy,
    health: list[ProviderHealth],
) -> dict[str, object]:
    selected = eligible_providers(tuple(health), policy)
    return {"providers": list(selected), "count": len(selected)}
