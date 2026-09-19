"""Runtime status and lifecycle introspection."""
from fastapi import APIRouter, Request
from cfip.application.runtime_supervisor import RuntimeSupervisor

router = APIRouter(prefix="/runtime", tags=["runtime"])

def _supervisor(request: Request) -> RuntimeSupervisor:
    value = getattr(request.app.state, "runtime_supervisor", None)
    if value is None:
        raise RuntimeError("runtime supervisor is not initialized")
    return value

@router.get("/status")
async def status(request: Request) -> dict[str, object]:
    return _supervisor(request).snapshot().model_dump()
