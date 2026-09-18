"""Governed platform-intelligence endpoints."""

from datetime import UTC, datetime
from fastapi import APIRouter

from cfip.application.intelligence import IntelligenceService
from cfip.domain.intelligence import IntelligenceEvidence, IntelligenceFeedback, IntelligenceProposal, IntelligenceSnapshot, LearningRecord

router = APIRouter(prefix="/intelligence")


@router.post("/snapshot", response_model=IntelligenceSnapshot)
async def intelligence_snapshot(
    evidence: list[IntelligenceEvidence],
    lessons: list[LearningRecord],
    proposals: list[IntelligenceProposal],
    calibration_score: float | None = None,
) -> IntelligenceSnapshot:
    return IntelligenceService().snapshot(
        evidence=evidence,
        lessons=lessons,
        proposals=proposals,
        calibration_score=calibration_score,
        as_of=int(datetime.now(UTC).timestamp()),
    )


@router.post("/feedback", response_model=IntelligenceFeedback)
async def intelligence_feedback(feedback: IntelligenceFeedback) -> IntelligenceFeedback:
    """Accept a governed feedback event; persistence/promotion is handled by the learning store."""
    return feedback
