"""Persistence adapter for signal lifecycle and outcome analytics."""

from decimal import Decimal

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from cfip.domain.outcomes import OutcomeLabelResult, SignalLifecycle
from cfip.infrastructure.db.models import (
    CalibrationReportModel,
    DriftReportModel,
    SignalLifecycleModel,
    SignalOutcomeEventModel,
    SignalOutcomeModel,
)


class OutcomeRepository:
    def __init__(self, session: AsyncSession) -> None:
        self.session = session

    async def upsert_signal(
        self,
        signal: SignalLifecycle,
        *,
        analysis_id: str | None = None,
        entry: float | None = None,
        stop: float | None = None,
        tp1: float | None = None,
        tp2: float | None = None,
        tp3: float | None = None,
        evidence_ids: list[str] | None = None,
    ) -> None:
        existing = await self.session.scalar(
            select(SignalLifecycleModel).where(
                SignalLifecycleModel.signal_id == signal.signal_id
            )
        )
        if existing is None:
            self.session.add(
                SignalLifecycleModel(
                    signal_id=signal.signal_id,
                    symbol=signal.symbol,
                    timeframe=signal.timeframe,
                    direction=signal.direction,
                    decision_time=signal.decision_time,
                    state=signal.state,
                    emitted_at=signal.emitted_at,
                    triggered_at=signal.triggered_at,
                    closed_at=signal.closed_at,
                    expires_at=signal.expires_at,
                    analysis_id=analysis_id,
                    entry=Decimal(str(entry)) if entry is not None else None,
                    stop=Decimal(str(stop)) if stop is not None else None,
                    tp1=Decimal(str(tp1)) if tp1 is not None else None,
                    tp2=Decimal(str(tp2)) if tp2 is not None else None,
                    tp3=Decimal(str(tp3)) if tp3 is not None else None,
                    evidence_ids=evidence_ids or [],
                )
            )
            return
        if (
            existing.symbol != signal.symbol
            or existing.timeframe != signal.timeframe
            or existing.direction != signal.direction
            or existing.decision_time != signal.decision_time
        ):
            raise ValueError("signal_id_conflict")
        existing.state = signal.state
        existing.emitted_at = signal.emitted_at
        existing.triggered_at = signal.triggered_at
        existing.closed_at = signal.closed_at
        existing.expires_at = signal.expires_at

    async def append_event(
        self,
        *,
        event_key: str,
        signal_id: str,
        event_type: str,
        event_time: int,
        price: float | None,
        payload: dict,
    ) -> bool:
        existing = await self.session.scalar(
            select(SignalOutcomeEventModel.id).where(
                SignalOutcomeEventModel.event_key == event_key
            )
        )
        if existing is not None:
            return False
        self.session.add(
            SignalOutcomeEventModel(
                event_key=event_key,
                signal_id=signal_id,
                event_type=event_type,
                event_time=event_time,
                price=Decimal(str(price)) if price is not None else None,
                payload=payload,
            )
        )
        return True

    async def record_outcome(self, result: OutcomeLabelResult) -> bool:
        existing = await self.session.scalar(
            select(SignalOutcomeModel.id).where(
                SignalOutcomeModel.signal_id == result.signal_id
            )
        )
        if existing is not None:
            return False
        self.session.add(
            SignalOutcomeModel(
                signal_id=result.signal_id,
                label=result.label,
                event_type=result.event,
                decision_time=result.decision_time,
                evaluation_time=result.evaluation_time,
                entry=Decimal(str(result.entry)),
                stop=Decimal(str(result.stop)) if result.stop is not None else None,
                tp1=Decimal(str(result.tp1)) if result.tp1 is not None else None,
                tp2=Decimal(str(result.tp2)) if result.tp2 is not None else None,
                tp3=Decimal(str(result.tp3)) if result.tp3 is not None else None,
                mfe_r=Decimal(str(result.mfe_r)) if result.mfe_r is not None else None,
                mae_r=Decimal(str(result.mae_r)) if result.mae_r is not None else None,
                realized_r=(
                    Decimal(str(result.realized_r))
                    if result.realized_r is not None
                    else None
                ),
                attribution=result.attribution,
                evidence_ids=result.evidence_ids,
            )
        )
        return True

    def add_calibration(self, *, as_of: int, report: dict) -> None:
        self.session.add(
            CalibrationReportModel(
                as_of=as_of,
                sample_count=report["sample_count"],
                brier_score=report.get("brier_score"),
                log_loss=report.get("log_loss"),
                expected_calibration_error=report.get("expected_calibration_error"),
                bins=report.get("bins", []),
            )
        )

    def add_drift(self, *, as_of: int, report: dict) -> None:
        self.session.add(
            DriftReportModel(
                as_of=as_of,
                baseline_count=report["baseline_count"],
                current_count=report["current_count"],
                minimum_sample_count=report["minimum_sample_count"],
                score=report.get("score"),
                detected=report["detected"],
                reason=report["reason"],
            )
        )
