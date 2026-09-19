"""Optional Redis cache adapter with an explicit connection lifecycle."""
from redis.asyncio import Redis

class RedisCache:
    def __init__(self, url: str) -> None:
        self.url = url
        self._client: Redis | None = None

    async def connect(self) -> None:
        if self._client is None:
            client = Redis.from_url(self.url)
            await client.ping()
            self._client = client

    async def close(self) -> None:
        if self._client is not None:
            await self._client.aclose()
            self._client = None

    async def get(self, key: str) -> bytes | None:
        if self._client is None:
            raise RuntimeError("Redis cache is not connected")
        return await self._client.get(key)

    async def set(self, key: str, value: bytes, ttl_seconds: int | None = None) -> None:
        if self._client is None:
            raise RuntimeError("Redis cache is not connected")
        await self._client.set(key, value, ex=ttl_seconds)

    async def delete(self, key: str) -> None:
        if self._client is None:
            raise RuntimeError("Redis cache is not connected")
        await self._client.delete(key)
