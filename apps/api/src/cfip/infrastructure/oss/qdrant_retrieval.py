"""Optional Qdrant retrieval adapter with a vendor-neutral result shape."""
from __future__ import annotations

from collections.abc import Sequence
from typing import Any


class QdrantRetrievalAdapter:
    provider_id = "qdrant"

    def __init__(self, url: str, collection: str, *, api_key: str | None = None) -> None:
        self._url = url
        self._collection = collection
        self._api_key = api_key
        self._client: Any | None = None

    async def _get_client(self) -> Any:
        if self._client is None:
            try:
                from qdrant_client import AsyncQdrantClient
            except ImportError as exc:
                raise RuntimeError("oss_dependency_missing:qdrant-client") from exc
            self._client = AsyncQdrantClient(url=self._url, api_key=self._api_key)
        return self._client

    async def upsert(self, records: Sequence[dict[str, Any]]) -> None:
        client = await self._get_client()
        try:
            from qdrant_client.models import PointStruct
        except ImportError as exc:
            raise RuntimeError("oss_dependency_missing:qdrant-client") from exc
        points = [
            PointStruct(
                id=str(record["id"]),
                vector=record["vector"],
                payload=record.get("payload", {}),
            )
            for record in records
        ]
        if points:
            await client.upsert(collection_name=self._collection, points=points, wait=True)

    async def query(self, query: str, *, limit: int = 10) -> Sequence[dict[str, Any]]:
        raise NotImplementedError(
            "qdrant_query_requires_embedding_provider:inject_embedding_and_query_contract"
        )

    async def close(self) -> None:
        if self._client is not None:
            await self._client.close()
            self._client = None
