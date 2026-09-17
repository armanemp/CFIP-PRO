"""Durable JetStream consumer boundary for market events."""

from collections.abc import Awaitable, Callable

from nats.js.api import AckPolicy, ConsumerConfig
from nats.js.client import JetStreamContext, Msg, PullSubscription

from cfip.infrastructure.messaging.jetstream import STREAM_NAME

EventHandler = Callable[[Msg], Awaitable[None]]


class MarketEventConsumer:
    """Pull consumer with explicit acknowledgements and bounded redelivery."""

    def __init__(self, jetstream: JetStreamContext) -> None:
        self._jetstream = jetstream
        self._subscription: PullSubscription | None = None

    async def start(self) -> None:
        if self._subscription is not None:
            return
        config = ConsumerConfig(
            durable_name="cfip-market-consumer",
            ack_policy=AckPolicy.EXPLICIT,
            max_deliver=5,
            filter_subject="market.>",
        )
        self._subscription = await self._jetstream.pull_subscribe(
            "market.>",
            durable=config.durable_name,
            stream=STREAM_NAME,
            config=config,
        )

    async def consume_once(self, handler: EventHandler, *, batch: int = 10, timeout: float = 1.0) -> int:
        if self._subscription is None:
            await self.start()
        assert self._subscription is not None
        messages = await self._subscription.fetch(batch=batch, timeout=timeout)
        for message in messages:
            try:
                await handler(message)
            except Exception:
                await message.nak()
                raise
            else:
                await message.ack()
        return len(messages)

    async def close(self) -> None:
        if self._subscription is not None:
            await self._subscription.unsubscribe()
            self._subscription = None
