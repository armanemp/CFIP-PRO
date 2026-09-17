"""Persistence operations for instruments, observations and the outbox."""

from datetime import datetime
from uuid import UUID

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from cfip.domain.market import InstrumentCreate, MarketObservationCreate, MarketObservationQuery
from cfip.infrastructure.db.models import InstrumentModel, MarketObservationModel, OutboxEventModel


class MarketRepository:
    def __init__(self, session: AsyncSession) -> None:
        self.session = session

    async def create_instrument(self, command: InstrumentCreate) -> InstrumentModel:
        model = InstrumentModel(**command.model_dump())
        self.session.add(model)
        await self.session.flush()
        return model

    async def get_instrument(self, instrument_id: UUID) -> InstrumentModel | None:
        return await self.session.get(InstrumentModel, instrument_id)

    async def list_instruments(self) -> list[InstrumentModel]:
        result = await self.session.execute(
            select(InstrumentModel).where(InstrumentModel.is_active.is_(True)).order_by(InstrumentModel.symbol)
        )
        return list(result.scalars().all())

    async def add_observation(self, command: MarketObservationCreate) -> MarketObservationModel:
        model = MarketObservationModel(**command.model_dump())
        self.session.add(model)
        await self.session.flush()
        return model

    async def list_observations(self, query: MarketObservationQuery) -> list[MarketObservationModel]:
        statement = (
            select(MarketObservationModel)
            .join(MarketObservationModel.instrument)
            .where(
                InstrumentModel.symbol == query.symbol,
                InstrumentModel.venue == query.venue,
            )
            .order_by(MarketObservationModel.observed_at.asc())
            .limit(query.limit)
        )
        if query.start is not None:
            statement = statement.where(MarketObservationModel.observed_at >= query.start)
        if query.end is not None:
            statement = statement.where(MarketObservationModel.observed_at <= query.end)
        result = await self.session.execute(statement)
        return list(result.scalars().all())

    def add_outbox_event(
        self,
        *,
        event_id: UUID,
        event_name: str,
        event_version: int,
        subject: str,
        occurred_at: datetime,
        payload: dict,
    ) -> OutboxEventModel:
        model = OutboxEventModel(
            id=event_id,
            event_name=event_name,
            event_version=event_version,
            subject=subject,
            occurred_at=occurred_at,
            payload=payload,
        )
        self.session.add(model)
        return model
