"""Small process-local event bus boundary.

The contract is intentionally independent of NATS so the runtime can boot without an
external broker. A NATS adapter can implement the same publish/subscribe semantics.
"""
import asyncio
from collections import defaultdict
from collections.abc import AsyncIterator
from typing import Any

class EventBus:
    def __init__(self) -> None:
        self._topics: dict[str, set[asyncio.Queue[dict[str, Any]]]] = defaultdict(set)
        self._lock = asyncio.Lock()

    async def publish(self, topic: str, payload: dict[str, Any]) -> None:
        async with self._lock:
            queues = tuple(self._topics.get(topic, ()))
        for queue in queues:
            queue.put_nowait(payload)

    async def subscribe(self, topic: str) -> AsyncIterator[dict[str, Any]]:
        queue: asyncio.Queue[dict[str, Any]] = asyncio.Queue(maxsize=256)
        async with self._lock:
            self._topics[topic].add(queue)
        try:
            while True:
                yield await queue.get()
        finally:
            async with self._lock:
                self._topics[topic].discard(queue)
                if not self._topics[topic]:
                    self._topics.pop(topic, None)
