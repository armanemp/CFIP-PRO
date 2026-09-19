"""Composable capability manifest for terminal/admin clients."""
from fastapi import APIRouter
from cfip.domain.platform_capabilities import capabilities
from cfip.domain.provider_contracts import PROVIDER_CATALOG

router=APIRouter(prefix="/platform",tags=["platform"])

@router.get("/manifest")
async def platform_manifest()->dict[str,object]:
    return {
        "schema_version":1,
        "capabilities":[x.model_dump() for x in capabilities()],
        "providers":[x.model_dump() for x in PROVIDER_CATALOG],
        "governance":{"mutations_require_policy":True,"secrets_exposed":False},
    }
