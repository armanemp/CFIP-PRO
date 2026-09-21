"""JetStream stream and publish semantics for market events."""

from nats.aio.client import Client as NATS
from nats.js import JetStreamContext

from cfip.core.config import get_settings

STREAM_NAME = "CFIP_MARKET"
STREAM_SUBJECTS = ["market.>"]


class JetStreamPublisher:
    def __init__(self, client: NATS) -> None:
        self._client = client
        self._jetstream: JetStreamContext = client.jetstream()

    async def ensure_stream(self) -> None:
        try:
            await self._jetstream.stream_info(STREAM_NAME)
        except Exception:
            await self._jetstream.add_stream(name=STREAM_NAME, subjects=STREAM_SUBJECTS)

    async def publish(self, subject: str, payload: bytes, *, message_id: str) -> None:
        await self._jetstream.publish(subject, payload, headers={"Nats-Msg-Id": message_id})


async def connect_jetstream() -> tuple[NATS, JetStreamPublisher]:
    client = NATS()
    await client.connect(get_settings().nats_url)
    publisher = JetStreamPublisher(client)
    await publisher.ensure_stream()
    return client, publisher
