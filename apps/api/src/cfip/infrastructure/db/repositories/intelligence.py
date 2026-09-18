"""Persistent governed intelligence store with idempotent audit events."""

from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from cfip.domain.intelligence import (
    IntelligenceEvidence,
    IntelligenceFeedback,
    IntelligenceProposal,
    LearningRecord,
)
from cfip.infrastructure.db.models import (
    IntelligenceAuditEventModel,
    IntelligenceEvidenceModel,
    IntelligenceFeedbackModel,
    IntelligenceProposalModel,
    LearningRecordModel,
)


class IntelligenceRepository:
    def __init__(self, session: AsyncSession) -> None:
        self.session = session

    async def upsert_evidence(self, item: IntelligenceEvidence) -> None:
        existing = await self.session.get(IntelligenceEvidenceModel, item.id)
        if existing is None:
            self.session.add(
                IntelligenceEvidenceModel(
                    id=item.id,
                    kind=item.kind,
                    source=item.source,
                    observed_at=item.observed_at,
                    content_hash=item.content_hash,
                    freshness_seconds=item.freshness_seconds,
                    confidence=item.confidence,
                    facts=item.facts,
                )
            )
            return
        if existing.content_hash != item.content_hash:
            raise ValueError("evidence_id_conflict")

    async def upsert_learning(self, item: LearningRecord) -> None:
        existing = await self.session.get(LearningRecordModel, item.id)
        if existing is None:
            self.session.add(
                LearningRecordModel(
                    id=item.id,
                    learning_type=item.type,
                    created_at_epoch=item.created_at,
                    subject=item.subject,
                    evidence_ids=item.evidence_ids,
                    lesson=item.lesson,
                    confidence=item.confidence,
                    status=item.status,
                    validation_count=item.validation_count,
                )
            )
            return
        if existing.lesson != item.lesson or existing.learning_type != item.type:
            raise ValueError("learning_id_conflict")

    async def upsert_proposal(self, item: IntelligenceProposal) -> None:
        existing = await self.session.get(IntelligenceProposalModel, item.id)
        if existing is None:
            self.session.add(
                IntelligenceProposalModel(
                    id=item.id,
                    kind=item.kind,
                    title=item.title,
                    rationale=item.rationale,
                    evidence_ids=item.evidence_ids,
                    risk_level=item.risk_level,
                    requires_approval=item.requires_approval,
                    status=item.status,
                )
            )
            return
        if existing.kind != item.kind or existing.title != item.title:
            raise ValueError("proposal_id_conflict")

    async def record_feedback(self, item: IntelligenceFeedback) -> None:
        learning = await self.session.get(LearningRecordModel, item.learning_id)
        if learning is None:
            raise ValueError("learning_not_found")
        self.session.add(
            IntelligenceFeedbackModel(
                learning_id=item.learning_id,
                accepted=item.accepted,
                reviewer=item.reviewer,
                reason=item.reason,
                observed_at=item.observed_at,
            )
        )
        learning.validation_count += 1
        if item.accepted:
            learning.status = "validated"
        elif learning.status == "candidate":
            learning.status = "rejected"

    async def append_audit(
        self,
        *,
        event_key: str,
        event_type: str,
        aggregate_type: str,
        aggregate_id: str,
        payload: dict,
        occurred_at: int,
    ) -> bool:
        existing = await self.session.scalar(
            select(IntelligenceAuditEventModel.id).where(
                IntelligenceAuditEventModel.event_key == event_key
            )
        )
        if existing is not None:
            return False
        self.session.add(
            IntelligenceAuditEventModel(
                event_key=event_key,
                event_type=event_type,
                aggregate_type=aggregate_type,
                aggregate_id=aggregate_id,
                payload=payload,
                occurred_at=occurred_at,
            )
        )
        return True

    async def counts(self) -> tuple[int, int, int, int]:
        evidence = await self.session.scalar(
            select(func.count()).select_from(IntelligenceEvidenceModel)
        )
        active_lessons = await self.session.scalar(
            select(func.count()).select_from(LearningRecordModel).where(
                LearningRecordModel.status.in_(("candidate", "validated"))
            )
        )
        validated_lessons = await self.session.scalar(
            select(func.count()).select_from(LearningRecordModel).where(
                LearningRecordModel.status == "validated"
            )
        )
        open_proposals = await self.session.scalar(
            select(func.count()).select_from(IntelligenceProposalModel).where(
                IntelligenceProposalModel.status == "proposed"
            )
        )
        return (
            int(evidence or 0),
            int(active_lessons or 0),
            int(validated_lessons or 0),
            int(open_proposals or 0),
        )
