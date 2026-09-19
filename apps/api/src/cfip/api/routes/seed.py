"""Safe bootstrap metadata. Secret material is never returned."""
from fastapi import APIRouter
from cfip.domain.seed_contracts import DEFAULT_SEEDS

router = APIRouter(prefix="/bootstrap", tags=["bootstrap"])


@router.get("/manifest")
async def manifest() -> dict[str, object]:
    return {
        "schema_version": DEFAULT_SEEDS.schema_version,
        "identities": [
            {"key": item.key, "role": item.role, "display_name": item.display_name}
            for item in DEFAULT_SEEDS.identities
        ],
        "plans": list(DEFAULT_SEEDS.plans),
        "secret_refs": [item.secret_ref for item in DEFAULT_SEEDS.identities],
    }
