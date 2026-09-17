"""HTTP transport for normalized market observations."""

from datetime import datetime
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Query, status
from sqlalchemy.exc import IntegrityError
from sqlalchemy.ext.asyncio import AsyncSession

from cfip.application.market import MarketService
from cfip.domain.market import (
    InstrumentCreate,
    InstrumentRead,
    MarketObservationCreate,
    MarketObservationQuery,
    MarketObservationRead,
)
from cfip.infrastructure.db.session import get_session

router = APIRouter(prefix="/market")


def get_market_service(session: Annotated[AsyncSession, Depends(get_session)]) -> MarketService:
    return MarketService(session)


MarketServiceDependency = Annotated[MarketService, Depends(get_market_service)]


@router.post("/instruments", response_model=InstrumentRead, status_code=status.HTTP_201_CREATED)
async def create_instrument(
    command: InstrumentCreate,
    service: MarketServiceDependency,
) -> InstrumentRead:
    try:
        return await service.create_instrument(command)
    except IntegrityError as exc:
        await service.session.rollback()
        raise HTTPException(status_code=409, detail="instrument_conflict") from exc


@router.get("/instruments", response_model=list[InstrumentRead])
async def list_instruments(service: MarketServiceDependency) -> list[InstrumentRead]:
    return await service.list_instruments()


@router.post(
    "/observations",
    response_model=MarketObservationRead,
    status_code=status.HTTP_201_CREATED,
)
async def ingest_observation(
    command: MarketObservationCreate,
    service: MarketServiceDependency,
) -> MarketObservationRead:
    try:
        return await service.ingest_observation(command)
    except ValueError as exc:
        await service.session.rollback()
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except IntegrityError as exc:
        await service.session.rollback()
        raise HTTPException(status_code=409, detail="observation_conflict") from exc


@router.get("/observations", response_model=list[MarketObservationRead])
async def list_observations(
    symbol: str = Query(min_length=1, max_length=64),
    venue: str = Query(min_length=1, max_length=64),
    start: datetime | None = None,
    end: datetime | None = None,
    limit: int = Query(default=500, ge=1, le=5000),
    service: MarketServiceDependency = None,
) -> list[MarketObservationRead]:
    query = MarketObservationQuery(
        symbol=symbol,
        venue=venue,
        start=start,
        end=end,
        limit=limit,
    )
    return await service.list_observations(query)
