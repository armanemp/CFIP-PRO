"""Deterministic training-data preparation for platform intelligence.

This layer prepares auditable examples; it does not silently train or promote a model.
Training and promotion remain separately governed operations.
"""

from cfip.domain.analysis import UnifiedAnalysisRead
from cfip.domain.intelligence import IntelligenceEvidence, TrainingExample


class TrainingPreparationService:
    def from_analysis(
        self,
        analysis: UnifiedAnalysisRead,
        *,
        evidence: list[IntelligenceEvidence],
        example_id: str,
    ) -> TrainingExample:
        evidence_ids = {item.id for item in evidence}
        usable = [item.id for item in evidence if item.id in evidence_ids]
        quality = min(
            1.0,
            sum(item.confidence for item in evidence) / max(1, len(evidence)),
        )
        features = {
            "analysis_score": analysis.score,
            "analysis_confidence": analysis.confidence,
            "confluence_score": analysis.confluence_score / 100.0,
            "trend_score": self._module_score(analysis, "trend"),
            "momentum_score": self._module_score(analysis, "momentum"),
            "structure_score": self._module_score(analysis, "structure"),
            "fvg_count": float(len(analysis.fvg_states)),
            "order_block_count": float(len(analysis.order_blocks)),
            "liquidity_count": float(len(analysis.liquidity_pools)),
            "mtf_aligned_count": float(sum(
                item.bias == analysis.bias and item.confidence > 0
                for item in analysis.mtf_contexts
            )),
        }
        return TrainingExample(
            id=example_id,
            subject=analysis.symbol,
            feature_vector=features,
            target=analysis.bias,
            evidence_ids=usable,
            label_quality=quality,
        )

    @staticmethod
    def _module_score(analysis: UnifiedAnalysisRead, name: str) -> float:
        for module in analysis.modules:
            if module.module == name:
                return module.score
        return 0.0
