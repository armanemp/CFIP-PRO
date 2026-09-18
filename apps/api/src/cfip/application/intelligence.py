"""Application services for provider-agnostic platform intelligence."""

from cfip.domain.intelligence import IntelligenceEvidence, LearningRecord, IntelligenceProposal, IntelligenceSnapshot


class IntelligenceService:
    """Build auditable intelligence artifacts without allowing silent model promotion."""

    def snapshot(
        self,
        *,
        evidence: list[IntelligenceEvidence],
        lessons: list[LearningRecord],
        proposals: list[IntelligenceProposal],
        calibration_score: float | None,
        as_of: int,
    ) -> IntelligenceSnapshot:
        return IntelligenceSnapshot(
            as_of=as_of,
            evidence_count=len(evidence),
            active_lessons=sum(item.status in {"candidate", "validated"} for item in lessons),
            validated_lessons=sum(item.status == "validated" for item in lessons),
            open_proposals=sum(item.status == "proposed" for item in proposals),
            calibration_score=calibration_score,
        )
