"""MIOS autonomy policy inspection endpoint."""
from fastapi import APIRouter
from pydantic import BaseModel, Field
from cfip.application.autonomy import AutonomyService
from cfip.domain.autonomy_policy import Action, Risk

router = APIRouter(prefix="/autonomy", tags=["intelligence"])

class AutonomyRequest(BaseModel):
    action: Action
    risk: Risk = "low"
    paths: tuple[str, ...] = ()
    changed_files: int = Field(default=1, ge=1, le=1000)

@router.post("/evaluate")
async def evaluate(request: AutonomyRequest) -> dict[str, object]:
    decision = AutonomyService().evaluate(
        action=request.action, risk=request.risk, paths=request.paths, changed_files=request.changed_files
    )
    return decision.model_dump()
