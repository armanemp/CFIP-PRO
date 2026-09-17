"""Real PostgreSQL integration coverage for the market-data vertical slice."""

from __future__ import annotations

import os
from datetime import UTC, datetime
from decimal import Decimal
from uuid import uuid4

import pytest
from sqlalchemy import delete, select
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker, create_async_engine

from cfip.application.market import MarketService
from cfip.domain.market import InstrumentCreate, MarketObservationCreate
from cfip.infrastructure.db.models import InstrumentModel, MarketObservationModel, OutboxEventModel

pytestmark = pytest.mark.integration


@pytest.fixture
async def integration_session_factory() -> async_sessionmaker[AsyncSession]:
    database_url = os.getenv("CFIP_TEST_DATABASE_URL")
    if not database_url:
        pytest.skip("CFIP_TEST_DATABASE_URL is required for PostgreSQL integration tests")

    engine = create_async_engine(database_url, pool_pre_ping=True)
    factory = async_sessionmaker(engine, expire_on_commit=False)
    try:
        yield factory
    finally:
        await engine.dispose()


@pytest.mark.asyncio
async def test_market_service_persists_observation_and_outbox_atomically(
    integration_session_factory: async_sessionmaker[AsyncSession],
) -> None:
    symbol = f"CFIPTEST-{uuid4().hex[:12]}"
    source_event_id = uuid4().hex
    observed_at = datetime(2026, 9, 17, 12, 0, tzinfo=UTC)

    async with integration_session_factory() as session:
        service = MarketService(session)
        instrument = await service.create_instrument(
            InstrumentCreate(
                symbol=symbol,
                asset_class="test",
                venue="integration",
                base_currency="AAA",
                quote_currency="BBB",
            )
        )

        observation = await service.ingest_observation(
            MarketObservationCreate(
                instrument_id=instrument.id,
                observed_at=observed_at,
                bid=Decimal("1.10001"),
                ask=Decimal("1.10003"),
                last=Decimal("1.10002"),
                volume=Decimal("1000"),
                source="integration-test",
                source_event_id=source_event_id,
            )
        )

        stored_observation = await session.get(MarketObservationModel, observation.id)
        assert stored_observation is not None
        assert stored_observation.instrument_id == instrument.id
        assert stored_observation.source_event_id == source_event_id

        result = await session.execute(
            select(OutboxEventModel).where(
                OutboxEventModel.subject == "market.observation.recorded",
                OutboxEventModel.payload["payload"]["observation_id"].as_string()
                == str(observation.id),
            )
        )
        outbox_event = result.scalar_one()
        assert outbox_event.event_name == "market.observation.recorded"
        assert outbox_event.event_version == 1
        assert outbox_event.payload["payload"]["instrument_id"] == str(instrument.id)

        await session.execute(delete(OutboxEventModel).where(OutboxEventModel.id == outbox_event.id))
        await session.execute(
            delete(MarketObservationModel).where(MarketObservationModel.id == observation.id)
        )
        await session.execute(delete(InstrumentModel).where(InstrumentModel.id == instrument.id))
        await session.commit()
