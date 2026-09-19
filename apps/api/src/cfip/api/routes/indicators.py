"""Indicator catalog and calculation API."""
from fastapi import APIRouter
from pydantic import BaseModel, ConfigDict, Field

from cfip.application.indicator_service import IndicatorInput, IndicatorService
from cfip.domain.indicator_contracts import IndicatorInstance, IndicatorPoint

router = APIRouter(prefix="/indicators", tags=["indicators"])


class IndicatorCalculationRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    instance: IndicatorInstance
    time: tuple[int, ...] = Field(min_length=1, max_length=10000)
    open: tuple[float, ...]
    high: tuple[float, ...]
    low: tuple[float, ...]
    close: tuple[float, ...]
    volume: tuple[float, ...]


@router.get("/definitions")
async def definitions() -> list[dict[str, object]]:
    return [item.model_dump() for item in IndicatorService().definitions()]


@router.post("/calculate", response_model=list[IndicatorPoint])
async def calculate(request: IndicatorCalculationRequest) -> list[IndicatorPoint]:
    data = IndicatorInput(
        time=request.time, open=request.open, high=request.high,
        low=request.low, close=request.close, volume=request.volume,
    )
    return list(IndicatorService().calculate(request.instance, data))
