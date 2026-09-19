"""HTTP transport for normalized market observations and provider diagnostics."""

from datetime import datetime
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Query, Request, status
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
from cfip.infrastructure.providers.ccxt_public import fetch_public_ohlcv
from cfip.infrastructure.providers.eodhd_demo import fetch_demo_market

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
        result = await service.ingest_observation(command)\n        bus = getattr(request.app.state, "event_bus", None)\n        if bus is not None:\n            await bus.publish(f"market.quote.{command.instrument_id}", result.model_dump(mode="json"))\n        return result
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
    *,
    service: MarketServiceDependency,
) -> list[MarketObservationRead]:
    query = MarketObservationQuery(
        symbol=symbol,
        venue=venue,
        start=start,
        end=end,
        limit=limit,
    )
    return await service.list_observations(query)


@router.get("/demo/eurusd")
async def demo_eurusd(limit: int = Query(default=500, ge=50, le=1000)) -> dict[str, object]:
    """Return real EODHD demo history + current EUR/USD quote for terminal testing."""
    try:
        return await fetch_demo_market(limit)
    except Exception as exc:
        raise HTTPException(status_code=502, detail="demo_provider_unavailable") from exc


@router.get("/providers/ccxt/ohlcv")
async def ccxt_ohlcv(
    exchange: str = Query(min_length=1, max_length=32),
    symbol: str = Query(min_length=1, max_length=64),
    timeframe: str = Query(default="1m", min_length=1, max_length=16),
    limit: int = Query(default=500, ge=1, le=5000),
) -> dict[str, object]:
    """Return normalized public OHLCV from a CCXT-supported crypto venue."""
    try:
        return await fetch_public_ohlcv(exchange, symbol, timeframe, limit)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        raise HTTPException(status_code=502, detail="ccxt_provider_unavailable") from exc
