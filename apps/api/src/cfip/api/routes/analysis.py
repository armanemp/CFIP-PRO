"""Canonical analysis transport boundary.

The initial endpoint deliberately accepts a normalized analysis result. The computation
will move behind this boundary as the backend analysis modules are integrated, keeping
the terminal independent from provider/framework-specific implementations.
"""

from datetime import datetime, timezone

from fastapi import APIRouter

from cfip.domain.analysis import UnifiedAnalysisRead

router = APIRouter(prefix="/analysis")


@router.post("/unified", response_model=UnifiedAnalysisRead)
async def unified_analysis(result: UnifiedAnalysisRead) -> UnifiedAnalysisRead:
    """Validate and return one canonical decision envelope."""
    return result.model_copy(update={"as_of": datetime.now(timezone.utc).isoformat()})
