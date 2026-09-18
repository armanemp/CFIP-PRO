"""PostgreSQL persistence models for the first market-data slice."""

from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from typing import Any
from uuid import UUID, uuid4

from sqlalchemy import Boolean, DateTime, ForeignKey, Index, Numeric, String, UniqueConstraint, func
from sqlalchemy.dialects.postgresql import JSONB
from sqlalchemy.dialects.postgresql import UUID as PGUUID
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, relationship


class Base(DeclarativeBase):
    pass


class InstrumentModel(Base):
    __tablename__ = "instruments"
    __table_args__ = (UniqueConstraint("symbol", "venue", name="uq_instruments_symbol_venue"),)

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    symbol: Mapped[str] = mapped_column(String(64), nullable=False)
    asset_class: Mapped[str] = mapped_column(String(32), nullable=False)
    venue: Mapped[str] = mapped_column(String(64), nullable=False)
    base_currency: Mapped[str | None] = mapped_column(String(16))
    quote_currency: Mapped[str | None] = mapped_column(String(16))
    is_active: Mapped[bool] = mapped_column(Boolean, nullable=False, default=True)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )

    observations: Mapped[list[MarketObservationModel]] = relationship(back_populates="instrument")


class MarketObservationModel(Base):
    __tablename__ = "market_observations"
    __table_args__ = (
        Index("ix_market_observations_instrument_observed_at", "instrument_id", "observed_at"),
        UniqueConstraint(
            "instrument_id",
            "source",
            "source_event_id",
            name="uq_market_observations_source_event",
        ),
    )

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    instrument_id: Mapped[UUID] = mapped_column(
        PGUUID(as_uuid=True), ForeignKey("instruments.id", ondelete="RESTRICT"), nullable=False
    )
    observed_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    bid: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    ask: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    last: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    volume: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    source: Mapped[str] = mapped_column(String(64), nullable=False)
    source_event_id: Mapped[str | None] = mapped_column(String(128))
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )

    instrument: Mapped[InstrumentModel] = relationship(back_populates="observations")


class OutboxEventModel(Base):
    __tablename__ = "outbox_events"
    __table_args__ = (Index("ix_outbox_events_pending", "published_at", "created_at"),)

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    event_name: Mapped[str] = mapped_column(String(128), nullable=False)
    event_version: Mapped[int] = mapped_column(nullable=False, default=1)
    subject: Mapped[str] = mapped_column(String(256), nullable=False)
    payload: Mapped[dict[str, Any]] = mapped_column(JSONB, nullable=False)
    occurred_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )
    published_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True))
    attempts: Mapped[int] = mapped_column(nullable=False, default=0)
    last_error: Mapped[str | None] = mapped_column(String(2000))


class IntelligenceEvidenceModel(Base):
    __tablename__ = "intelligence_evidence"

    id: Mapped[str] = mapped_column(String(128), primary_key=True)
    kind: Mapped[str] = mapped_column(String(32), nullable=False)
    source: Mapped[str] = mapped_column(String(256), nullable=False)
    observed_at: Mapped[int] = mapped_column(nullable=False)
    content_hash: Mapped[str] = mapped_column(String(128), nullable=False)
    freshness_seconds: Mapped[int] = mapped_column(nullable=False)
    confidence: Mapped[float] = mapped_column(Numeric(6, 5), nullable=False)
    facts: Mapped[list[str]] = mapped_column(JSONB, nullable=False, default=list)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class LearningRecordModel(Base):
    __tablename__ = "learning_records"

    id: Mapped[str] = mapped_column(String(128), primary_key=True)
    learning_type: Mapped[str] = mapped_column(String(32), nullable=False)
    created_at_epoch: Mapped[int] = mapped_column(nullable=False)
    subject: Mapped[str] = mapped_column(String(256), nullable=False)
    evidence_ids: Mapped[list[str]] = mapped_column(JSONB, nullable=False, default=list)
    lesson: Mapped[str] = mapped_column(String(4000), nullable=False)
    confidence: Mapped[float] = mapped_column(Numeric(6, 5), nullable=False)
    status: Mapped[str] = mapped_column(String(32), nullable=False)
    validation_count: Mapped[int] = mapped_column(nullable=False, default=0)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class IntelligenceProposalModel(Base):
    __tablename__ = "intelligence_proposals"

    id: Mapped[str] = mapped_column(String(128), primary_key=True)
    kind: Mapped[str] = mapped_column(String(32), nullable=False)
    title: Mapped[str] = mapped_column(String(256), nullable=False)
    rationale: Mapped[str] = mapped_column(String(4000), nullable=False)
    evidence_ids: Mapped[list[str]] = mapped_column(JSONB, nullable=False)
    risk_level: Mapped[str] = mapped_column(String(16), nullable=False)
    requires_approval: Mapped[bool] = mapped_column(Boolean, nullable=False, default=True)
    status: Mapped[str] = mapped_column(String(32), nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class IntelligenceFeedbackModel(Base):
    __tablename__ = "intelligence_feedback"

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    learning_id: Mapped[str] = mapped_column(
        String(128), ForeignKey("learning_records.id", ondelete="RESTRICT"), nullable=False
    )
    accepted: Mapped[bool] = mapped_column(Boolean, nullable=False)
    reviewer: Mapped[str] = mapped_column(String(128), nullable=False)
    reason: Mapped[str] = mapped_column(String(2000), nullable=False)
    observed_at: Mapped[int] = mapped_column(nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class IntelligenceAuditEventModel(Base):
    __tablename__ = "intelligence_audit_events"
    __table_args__ = (
        UniqueConstraint("event_key", name="uq_intelligence_audit_event_key"),
        Index("ix_intelligence_audit_events_occurred_at", "occurred_at"),
    )

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    event_key: Mapped[str] = mapped_column(String(256), nullable=False)
    event_type: Mapped[str] = mapped_column(String(128), nullable=False)
    aggregate_type: Mapped[str] = mapped_column(String(64), nullable=False)
    aggregate_id: Mapped[str] = mapped_column(String(160), nullable=False)
    payload: Mapped[dict[str, Any]] = mapped_column(JSONB, nullable=False)
    occurred_at: Mapped[int] = mapped_column(nullable=False)
    previous_hash: Mapped[str | None] = mapped_column(String(128))
    event_hash: Mapped[str] = mapped_column(String(128), nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )
