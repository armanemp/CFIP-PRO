"""Optional NATS JetStream adapter; no connection is attempted at import time."""
from __future__ import annotations
import json
from collections.abc import AsyncIterator
from nats.aio.client import Client as NATS
from nats.js import JetStreamContext
from cfip.domain.event_contracts import EventEnvelope

class NatsJetStreamTransport:
    def __init__(self, url: str, stream: str = "CFIP_EVENTS") -> None:
        self.url = url
        self.stream = stream
        self._client: NATS | None = None
        self._js: JetStreamContext | None = None

    @property
    def connected(self) -> bool:
        return bool(self._client and self._client.is_connected)

    async def connect(self) -> None:
        if self.connected:
            return
        client = NATS()
        await client.connect(servers=[self.url], name="cfip-runtime")
        self._client = client
        self._js = client.jetstream()
        try:
            await self._js.add_stream(name=self.stream, subjects=["cfip.>"])
        except Exception:
            # Stream may already exist; publishing remains safe.
            pass

    async def close(self) -> None:
        if self._client is not None:
            await self._client.drain()
            self._client = None
            self._js = None

    async def publish(self, topic: str, event: EventEnvelope) -> None:
        if self._js is None:
            raise RuntimeError("NATS JetStream transport is not connected")
        await self._js.publish(f"cfip.{topic}", event.model_dump_json().encode())

    async def subscribe(self, topic: str) -> AsyncIterator[EventEnvelope]:
        if self._js is None:
            raise RuntimeError("NATS JetStream transport is not connected")
        subscription = await self._js.subscribe(f"cfip.{topic}")
        while True:
            message = await subscription.next_msg()
            yield EventEnvelope.model_validate(json.loads(message.data))
