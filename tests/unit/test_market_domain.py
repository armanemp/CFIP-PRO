from datetime import UTC, datetime
from decimal import Decimal
from uuid import uuid4

import pytest
from pydantic import ValidationError

from cfip.domain.events import EventEnvelope
from cfip.domain.market import InstrumentCreate, MarketObservationCreate, MarketObservationQuery


def test_instrument_contract_requires_symbol_and_venue() -> None:
    instrument = InstrumentCreate(symbol="EUR/USD", asset_class="forex", venue="reference")
    assert instrument.symbol == "EUR/USD"


def test_observation_contract_accepts_decimal_market_values() -> None:
    observation = MarketObservationCreate(
        instrument_id=uuid4(),
        observed_at=datetime(2026, 9, 17, 12, 0, tzinfo=UTC),
        bid=Decimal("1.10001"),
        ask=Decimal("1.10003"),
        last=Decimal("1.10002"),
        source="provider-a",
    )
    assert observation.last == Decimal("1.10002")


def test_observation_rejects_negative_price() -> None:
    with pytest.raises(ValidationError):
        MarketObservationCreate(
            instrument_id=uuid4(),
            bid=Decimal("-1"),
            source="provider-a",
        )


def test_query_limit_is_bounded() -> None:
    query = MarketObservationQuery(symbol="EUR/USD", venue="reference", limit=100)
    assert query.limit == 100


def test_event_envelope_serializes_uuid_and_datetime() -> None:
    event = EventEnvelope(event_name="market.observation.recorded", producer="cfip.market", payload={})
    encoded = event.model_dump(mode="json")
    assert encoded["event_name"] == "market.observation.recorded"
    assert isinstance(encoded["event_id"], str)
