from cfip.domain.event_contracts import EventEnvelope

def test_event_envelope_is_versioned_and_strict():
    event = EventEnvelope(event_id="e1", topic="market.quote", occurred_at=1, producer="market", payload={"symbol":"EUR/USD"})
    assert event.schema_version == 1
    assert event.payload["symbol"] == "EUR/USD"
