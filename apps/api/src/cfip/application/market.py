"""Application services for the first market-data vertical slice."""

from uuid import uuid4

from sqlalchemy.ext.asyncio import AsyncSession

from cfip.domain.events import EventEnvelope
from cfip.domain.market import InstrumentCreate, MarketObservationCreate, MarketObservationQuery
from cfip.infrastructure.db.repositories.market import MarketRepository


class MarketService:
    def __init__(self, session: AsyncSession) -> None:
        self.repository = MarketRepository(session)
        self.session = session

    async def create_instrument(self, command: InstrumentCreate):
        instrument = await self.repository.create_instrument(command)
        await self.session.commit()
        await self.session.refresh(instrument)
        return instrument

    async def list_instruments(self):
        return await self.repository.list_instruments()

    async def ingest_observation(self, command: MarketObservationCreate):
        instrument = await self.repository.get_instrument(command.instrument_id)
        if instrument is None or not instrument.is_active:
            raise ValueError("instrument_not_found_or_inactive")

        observation = await self.repository.add_observation(command)
        event = EventEnvelope(
            event_id=uuid4(),
            event_name="market.observation.recorded",
            event_version=1,
            producer="cfip.market",
            payload={
                "observation_id": str(observation.id),
                "instrument_id": str(observation.instrument_id),
                "observed_at": observation.observed_at.isoformat(),
                "source": observation.source,
                "source_event_id": observation.source_event_id,
            },
        )
        self.repository.add_outbox_event(
            event_id=event.event_id,
            event_name=event.event_name,
            event_version=event.event_version,
            subject="market.observation.recorded",
            occurred_at=event.occurred_at,
            payload=event.model_dump(mode="json"),
        )
        await self.session.commit()
        await self.session.refresh(observation)
        return observation

    async def list_observations(self, query: MarketObservationQuery):
        return await self.repository.list_observations(query)
