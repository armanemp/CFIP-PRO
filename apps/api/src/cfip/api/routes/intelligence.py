"""Governed platform-intelligence endpoints."""

from datetime import UTC, datetime
from typing import Annotated

from fastapi import APIRouter, Depends
from sqlalchemy.ext.asyncio import AsyncSession

from cfip.application.intelligence import IntelligenceService
from cfip.domain.intelligence import (
    IntelligenceEvidence,
    IntelligenceFeedback,
    IntelligenceProposal,
    IntelligenceSnapshot,
    LearningRecord,
)
from cfip.infrastructure.db.session import get_session

router = APIRouter(prefix="/intelligence")


@router.post("/snapshot", response_model=IntelligenceSnapshot)
async def intelligence_snapshot(
    evidence: list[IntelligenceEvidence],
    lessons: list[LearningRecord],
    proposals: list[IntelligenceProposal],
    calibration_score: float | None = None,
    session: Annotated[AsyncSession, Depends(get_session)] = None,
) -> IntelligenceSnapshot:
    service = IntelligenceService(session)
    await service.record_snapshot_inputs(
        evidence=evidence,
        lessons=lessons,
        proposals=proposals,
    )
    return await service.snapshot(
        calibration_score=calibration_score,
        as_of=int(datetime.now(UTC).timestamp()),
    )


@router.post("/feedback", response_model=IntelligenceFeedback)
async def intelligence_feedback(
    feedback: IntelligenceFeedback,
    session: Annotated[AsyncSession, Depends(get_session)],
) -> IntelligenceFeedback:
    return await IntelligenceService(session).feedback(feedback)
