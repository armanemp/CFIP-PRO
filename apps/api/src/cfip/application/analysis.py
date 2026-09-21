"""Application facade for deterministic market analysis."""

from cfip.domain.analysis import AnalysisRequest, UnifiedAnalysisRead
from cfip.infrastructure.analysis.engine import analyze


class AnalysisService:
    def analyze(self, request: AnalysisRequest, *, as_of: str) -> UnifiedAnalysisRead:
        return analyze(request, as_of)
