"""Canonical unified market-analysis endpoint."""

from datetime import UTC, datetime

from fastapi import APIRouter

from cfip.application.analysis import AnalysisService
from cfip.domain.analysis import AnalysisRequest, UnifiedAnalysisRead

router = APIRouter(prefix="/analysis")


@router.post("/unified", response_model=UnifiedAnalysisRead)
async def unified_analysis(request: AnalysisRequest) -> UnifiedAnalysisRead:
    """Compute one causal, closed-bar-aware decision envelope."""
    return AnalysisService().analyze(request, as_of=datetime.now(UTC).isoformat())
