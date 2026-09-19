from cfip.domain.event_contracts import EventEnvelope
from cfip.infrastructure.messaging.nats_transport import NatsJetStreamTransport
from cfip.infrastructure.cache.redis_cache import RedisCache

def test_nats_transport_is_lazy():
    transport = NatsJetStreamTransport("nats://127.0.0.1:4222")
    assert not transport.connected

def test_redis_cache_is_lazy():
    cache = RedisCache("redis://127.0.0.1:6379/0")
    assert cache._client is None

def test_event_envelope_remains_the_transport_payload():
    event = EventEnvelope(
        event_id="evt-1",
        event_name="market.tick",
        occurred_at=1,
        correlation_id="corr-1",
        producer="test",
        payload={"bid": 1.0},
    )
    assert event.model_dump()["payload"]["bid"] == 1.0
