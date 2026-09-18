"""Deterministic preparation of governed training examples."""

import hashlib
import json

from cfip.domain.analysis import UnifiedAnalysisRead
from cfip.domain.intelligence import IntelligenceEvidence, TrainingExample


class TrainingPreparationService:
    """Create reproducible, leakage-resistant examples without training or promotion."""

    @staticmethod
    def _module_score(analysis: UnifiedAnalysisRead, name: str) -> float:
        for module in analysis.modules:
            if module.module == name:
                return module.score
        return 0.0

    @staticmethod
    def from_analysis(
        analysis: UnifiedAnalysisRead,
        evidence: list[IntelligenceEvidence],
        *,
        outcome: str = "unknown",
        label_horizon_bars: int = 0,
    ) -> TrainingExample:
        evidence_ids = [item.id for item in evidence]
        if not evidence_ids:
            raise ValueError("training_example_requires_evidence")
        if analysis.closed_bar_time is None:
            raise ValueError("training_example_requires_closed_bar_time")

        features = {
            "analysis_score": analysis.score,
            "analysis_confidence": analysis.confidence,
            "confluence_score": float(analysis.confluence_score),
            "trend_score": TrainingPreparationService._module_score(analysis, "trend"),
            "momentum_score": TrainingPreparationService._module_score(analysis, "momentum"),
            "structure_score": TrainingPreparationService._module_score(
                analysis, "structure"
            ),
            "fvg_count": float(len(analysis.fvg_states)),
            "order_block_count": float(len(analysis.order_blocks)),
            "liquidity_count": float(len(analysis.liquidity_pools)),
            "mtf_aligned_count": float(
                sum(
                    item.bias == analysis.bias
                    for item in analysis.mtf_contexts
                    if item.bias != "neutral"
                )
            ),
        }
        feature_schema = {name: "float" for name in features}
        provenance = json.dumps(
            {
                "analysis_as_of": analysis.as_of,
                "analysis_timeframe": analysis.timeframe,
                "analysis_symbol": analysis.symbol,
                "closed_bar_time": analysis.closed_bar_time,
                "evidence_ids": evidence_ids,
                "feature_schema": feature_schema,
                "label_horizon_bars": label_horizon_bars,
            },
            sort_keys=True,
            separators=(",", ":"),
        )
        provenance_hash = hashlib.sha256(provenance.encode()).hexdigest()
        return TrainingExample(
            id=f"train-{provenance_hash[:32]}",
            subject=f"{analysis.symbol}:{analysis.timeframe}",
            as_of=analysis.closed_bar_time,
            timeframe=analysis.timeframe,
            feature_vector=features,
            feature_schema=feature_schema,
            target=analysis.bias,
            outcome=outcome,
            label_horizon_bars=label_horizon_bars,
            label_quality=0.0 if outcome == "unknown" else 1.0,
            evidence_ids=evidence_ids,
            provenance_hash=provenance_hash,
        )
