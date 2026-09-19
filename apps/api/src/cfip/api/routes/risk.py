"""Risk calculation endpoint. No broker order is submitted here."""
from fastapi import APIRouter

from cfip.application.risk_service import RiskService
from cfip.domain.risk_contracts import PositionSizingRequest, PositionSizingResult

router = APIRouter(prefix="/risk", tags=["risk"])


@router.post("/position-size", response_model=PositionSizingResult)
async def position_size(request: PositionSizingRequest) -> PositionSizingResult:
    return RiskService().size(request)
