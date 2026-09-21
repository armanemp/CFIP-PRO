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
    __table_args__ = (
        UniqueConstraint(
            "learning_id", "reviewer", "observed_at", name="uq_intelligence_feedback_idempotency"
        ),
    )

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


class SignalLifecycleModel(Base):
    __tablename__ = "signal_lifecycles"
    __table_args__ = (
        UniqueConstraint("signal_id", name="uq_signal_lifecycles_signal_id"),
        Index(
            "ix_signal_lifecycles_symbol_timeframe_decision",
            "symbol",
            "timeframe",
            "decision_time",
        ),
        Index("ix_signal_lifecycles_state", "state"),
    )

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    signal_id: Mapped[str] = mapped_column(String(160), nullable=False)
    symbol: Mapped[str] = mapped_column(String(64), nullable=False)
    timeframe: Mapped[str] = mapped_column(String(16), nullable=False)
    direction: Mapped[str] = mapped_column(String(8), nullable=False)
    decision_time: Mapped[int] = mapped_column(nullable=False)
    state: Mapped[str] = mapped_column(String(16), nullable=False)
    emitted_at: Mapped[int | None] = mapped_column()
    triggered_at: Mapped[int | None] = mapped_column()
    closed_at: Mapped[int | None] = mapped_column()
    expires_at: Mapped[int | None] = mapped_column()
    analysis_id: Mapped[str | None] = mapped_column(String(128))
    entry: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    stop: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    tp1: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    tp2: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    tp3: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    evidence_ids: Mapped[list[str]] = mapped_column(JSONB, nullable=False, default=list)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class SignalOutcomeEventModel(Base):
    __tablename__ = "signal_outcome_events"
    __table_args__ = (
        UniqueConstraint("event_key", name="uq_signal_outcome_events_event_key"),
        Index("ix_signal_outcome_events_signal_time", "signal_id", "event_time"),
    )

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    event_key: Mapped[str] = mapped_column(String(256), nullable=False)
    signal_id: Mapped[str] = mapped_column(
        String(160), ForeignKey("signal_lifecycles.signal_id", ondelete="RESTRICT"), nullable=False
    )
    event_type: Mapped[str] = mapped_column(String(16), nullable=False)
    event_time: Mapped[int] = mapped_column(nullable=False)
    price: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    payload: Mapped[dict[str, Any]] = mapped_column(JSONB, nullable=False, default=dict)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class SignalOutcomeModel(Base):
    __tablename__ = "signal_outcomes"
    __table_args__ = (
        UniqueConstraint("signal_id", name="uq_signal_outcomes_signal_id"),
        Index("ix_signal_outcomes_label_evaluation_time", "label", "evaluation_time"),
    )

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    signal_id: Mapped[str] = mapped_column(
        String(160), ForeignKey("signal_lifecycles.signal_id", ondelete="RESTRICT"), nullable=False
    )
    label: Mapped[str] = mapped_column(String(16), nullable=False)
    event_type: Mapped[str] = mapped_column(String(16), nullable=False)
    decision_time: Mapped[int] = mapped_column(nullable=False)
    evaluation_time: Mapped[int] = mapped_column(nullable=False)
    entry: Mapped[Decimal] = mapped_column(Numeric(38, 18), nullable=False)
    stop: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    tp1: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    tp2: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    tp3: Mapped[Decimal | None] = mapped_column(Numeric(38, 18))
    mfe_r: Mapped[Decimal | None] = mapped_column(Numeric(20, 10))
    mae_r: Mapped[Decimal | None] = mapped_column(Numeric(20, 10))
    realized_r: Mapped[Decimal | None] = mapped_column(Numeric(20, 10))
    attribution: Mapped[dict[str, float]] = mapped_column(JSONB, nullable=False, default=dict)
    evidence_ids: Mapped[list[str]] = mapped_column(JSONB, nullable=False, default=list)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class CalibrationReportModel(Base):
    __tablename__ = "calibration_reports"

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    as_of: Mapped[int] = mapped_column(nullable=False)
    sample_count: Mapped[int] = mapped_column(nullable=False)
    brier_score: Mapped[Decimal | None] = mapped_column(Numeric(20, 10))
    log_loss: Mapped[Decimal | None] = mapped_column(Numeric(20, 10))
    expected_calibration_error: Mapped[Decimal | None] = mapped_column(Numeric(20, 10))
    bins: Mapped[list[dict[str, Any]]] = mapped_column(JSONB, nullable=False, default=list)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class DriftReportModel(Base):
    __tablename__ = "drift_reports"

    id: Mapped[UUID] = mapped_column(PGUUID(as_uuid=True), primary_key=True, default=uuid4)
    as_of: Mapped[int] = mapped_column(nullable=False)
    baseline_count: Mapped[int] = mapped_column(nullable=False)
    current_count: Mapped[int] = mapped_column(nullable=False)
    minimum_sample_count: Mapped[int] = mapped_column(nullable=False)
    score: Mapped[Decimal | None] = mapped_column(Numeric(20, 10))
    detected: Mapped[bool] = mapped_column(nullable=False)
    reason: Mapped[str] = mapped_column(String(256), nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )


class PlatformSettingModel(Base):
    __tablename__ = "platform_settings"

    key: Mapped[str] = mapped_column(String(128), primary_key=True)
    value: Mapped[str] = mapped_column(String(512), nullable=False)
    updated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), onupdate=func.now(), nullable=False
    )
