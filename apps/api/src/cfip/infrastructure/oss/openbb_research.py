"""Optional OpenBB research adapter.

OpenBB is imported only when this adapter is instantiated/used because its
Python interface can initialize a large extension graph. Results are reduced
to plain dictionaries before crossing the CFIP infrastructure boundary.
"""
from __future__ import annotations

import asyncio
from collections.abc import Sequence
from typing import Any


class OpenBBResearchAdapter:
    provider_id = "openbb"

    def __init__(self, *, provider: str | None = None) -> None:
        self._provider = provider

    def _obb(self) -> Any:
        try:
            from openbb import obb
        except ImportError as exc:
            raise RuntimeError("oss_dependency_missing:openbb") from exc
        return obb

    @staticmethod
    def _serialize(result: Any) -> Sequence[dict[str, Any]]:
        values = getattr(result, "results", result)
        if values is None:
            return ()
        output: list[dict[str, Any]] = []
        for item in values:
            if hasattr(item, "model_dump"):
                output.append(item.model_dump(mode="json"))
            elif isinstance(item, dict):
                output.append(dict(item))
            else:
                output.append({"value": str(item)})
        return output

    async def search(self, query: str, limit: int = 10) -> Sequence[dict[str, Any]]:
        if not query.strip():
            raise ValueError("openbb_research_query_required")
        if not 1 <= limit <= 100:
            raise ValueError("openbb_research_limit_out_of_range")

        def _search() -> Sequence[dict[str, Any]]:
            obb = self._obb()
            kwargs: dict[str, Any] = {"term": query, "limit": limit}
            if self._provider:
                kwargs["provider"] = self._provider
            result = obb.news.world(**kwargs)
            return self._serialize(result)

        return await asyncio.to_thread(_search)
