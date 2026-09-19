"""Transport-neutral provider adapter contracts.

Adapters normalize external APIs into CFIP-owned contracts. Catalog membership never
implies that credentials, connectivity, quota, or live execution are available.
"""
from typing import Protocol
from cfip.domain.datafeed_contracts import DataFeedCapabilities, MarketDataPage, ProviderHealth
from cfip.domain.provider_contracts import ProviderDescriptor

class MarketDataProvider(Protocol):
    descriptor: ProviderDescriptor
    async def health(self) -> ProviderHealth: ...
    async def capabilities(self) -> DataFeedCapabilities: ...
    async def fetch(self, request: dict[str, object]) -> MarketDataPage: ...
