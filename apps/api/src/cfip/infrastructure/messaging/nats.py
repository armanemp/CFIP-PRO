"""NATS JetStream adapter boundary."""

from nats.aio.client import Client as NATS

from cfip.core.config import get_settings


class NatsPublisher:
    def __init__(self, client: NATS) -> None:
        self._client = client

    async def publish(self, subject: str, payload: bytes) -> None:
        await self._client.publish(subject, payload)


async def connect_nats() -> NATS:
    client = NATS()
    await client.connect(get_settings().nats_url)
    return client
