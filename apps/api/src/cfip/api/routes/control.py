"""Safe control-plane introspection endpoints.

Mutation is deliberately not exposed here until authentication, RBAC, persistence and
an immutable audit sink are wired together.
"""
from fastapi import APIRouter
from cfip.domain.control_plane import ControlAction

router = APIRouter(prefix="/control", tags=["control"])

@router.get("/actions")
async def actions() -> dict[str, object]:
    return {
        "actions": list(ControlAction.__args__),
        "mutation_boundary": "authenticated-rbac-audit",
        "dry_run_default": True,
    }
