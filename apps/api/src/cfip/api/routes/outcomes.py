"""Governed persistence endpoints for signal lifecycle and outcome analytics."""

from typing import Annotated

from fastapi import APIRouter, Depends
from pydantic import BaseModel, ConfigDict, Field
from sqlalchemy.ext.asyncio import AsyncSession

from cfip.domain.outcomes import OutcomeLabelResult, SignalLifecycle
from cfip.infrastructure.db.repositories.outcomes import OutcomeRepository
from cfip.infrastructure.db.session import get_session

router = APIRouter(prefix="/outcomes")


class SignalRecordRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    signal: SignalLifecycle
    analysis_id: str | None = Field(default=None, max_length=128)
    entry: float | None = Field(default=None, gt=0)
    stop: float | None = Field(default=None, gt=0)
    tp1: float | None = Field(default=None, gt=0)
    tp2: float | None = Field(default=None, gt=0)
    tp3: float | None = Field(default=None, gt=0)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)


class OutcomeEventRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    event_key: str = Field(min_length=1, max_length=256)
    signal_id: str = Field(min_length=1, max_length=160)
    event_type: str = Field(min_length=1, max_length=16)
    event_time: int = Field(gt=0)
    price: float | None = Field(default=None, gt=0)
    payload: dict = Field(default_factory=dict)


class OutcomeReportRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    as_of: int = Field(gt=0)
    report: dict


@router.post("/signals")
async def record_signal(
    request: SignalRecordRequest,
    session: Annotated[AsyncSession, Depends(get_session)],
) -> dict[str, str]:
    repository = OutcomeRepository(session)
    await repository.upsert_signal(
        request.signal,
        analysis_id=request.analysis_id,
        entry=request.entry,
        stop=request.stop,
        tp1=request.tp1,
        tp2=request.tp2,
        tp3=request.tp3,
        evidence_ids=request.evidence_ids,
    )
    await session.commit()
    return {"signal_id": request.signal.signal_id, "status": "stored"}


@router.post("/events")
async def record_event(
    request: OutcomeEventRequest,
    session: Annotated[AsyncSession, Depends(get_session)],
) -> dict[str, bool]:
    stored = await OutcomeRepository(session).append_event(
        event_key=request.event_key,
        signal_id=request.signal_id,
        event_type=request.event_type,
        event_time=request.event_time,
        price=request.price,
        payload=request.payload,
    )
    await session.commit()
    return {"stored": stored}


@router.post("/finalize")
async def finalize_outcome(
    result: OutcomeLabelResult,
    session: Annotated[AsyncSession, Depends(get_session)],
) -> dict[str, bool]:
    stored = await OutcomeRepository(session).record_outcome(result)
    await session.commit()
    return {"stored": stored}


@router.post("/calibration")
async def record_calibration(
    request: OutcomeReportRequest,
    session: Annotated[AsyncSession, Depends(get_session)],
) -> dict[str, str]:
    OutcomeRepository(session).add_calibration(as_of=request.as_of, report=request.report)
    await session.commit()
    return {"status": "stored"}


@router.post("/drift")
async def record_drift(
    request: OutcomeReportRequest,
    session: Annotated[AsyncSession, Depends(get_session)],
) -> dict[str, str]:
    OutcomeRepository(session).add_drift(as_of=request.as_of, report=request.report)
    await session.commit()
    return {"status": "stored"}
