"""Application services for provider-agnostic platform intelligence."""

from sqlalchemy.ext.asyncio import AsyncSession

from cfip.domain.intelligence import (
    IntelligenceEvidence,
    IntelligenceFeedback,
    IntelligenceProposal,
    IntelligenceSnapshot,
    LearningRecord,
)
from cfip.infrastructure.db.repositories.intelligence import IntelligenceRepository


class IntelligenceService:
    """Persist auditable learning artifacts without silent model promotion."""

    def __init__(self, session: AsyncSession) -> None:
        self.repository = IntelligenceRepository(session)
        self.session = session

    async def record_snapshot_inputs(
        self,
        *,
        evidence: list[IntelligenceEvidence],
        lessons: list[LearningRecord],
        proposals: list[IntelligenceProposal],
    ) -> None:
        for item in evidence:
            await self.repository.upsert_evidence(item)
        for item in lessons:
            await self.repository.upsert_learning(item)
        for item in proposals:
            await self.repository.upsert_proposal(item)
        await self.session.commit()

    async def feedback(self, item: IntelligenceFeedback) -> IntelligenceFeedback:
        await self.repository.record_feedback(item)
        await self.repository.append_audit(
            event_key=f"feedback:{item.learning_id}:{item.observed_at}:{item.reviewer}",
            event_type="intelligence.feedback.recorded",
            aggregate_type="learning",
            aggregate_id=item.learning_id,
            payload=item.model_dump(),
            occurred_at=item.observed_at,
        )
        await self.session.commit()
        return item

    async def snapshot(self, *, calibration_score: float | None, as_of: int) -> IntelligenceSnapshot:
        evidence, active_lessons, validated_lessons, open_proposals = await self.repository.counts()
        return IntelligenceSnapshot(
            as_of=as_of,
            evidence_count=evidence,
            active_lessons=active_lessons,
            validated_lessons=validated_lessons,
            open_proposals=open_proposals,
            calibration_score=calibration_score,
        )
