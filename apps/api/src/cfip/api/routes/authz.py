"""Authorization introspection boundary."""
from fastapi import APIRouter
from cfip.domain.auth_contracts import Principal, authorize

router = APIRouter(prefix="/authz", tags=["auth"])

@router.post("/check")
async def check(principal: Principal, permission: str) -> dict[str, object]:
    decision = authorize(principal, permission) if permission in {"terminal:read","terminal:trade","research:read","intelligence:read","intelligence:propose","git:read","git:propose","git:apply","admin:settings"} else None
    return decision.model_dump() if decision else {"allowed": False, "permission": permission, "reason": "unknown_permission"}
