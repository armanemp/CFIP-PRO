"""CFIP-owned contracts for optional OSS capability adapters.

The platform depends on these stable interfaces, never on vendor-specific
objects. Concrete OSS integrations live in infrastructure adapters.
"""

from collections.abc import Sequence
from typing import Any, Protocol, runtime_checkable


@runtime_checkable
class MarketDataAdapter(Protocol):
    provider_id: str
    async def candles(self, symbol: str, timeframe: str, limit: int) -> Sequence[dict[str, Any]]: ...


@runtime_checkable
class ResearchRetriever(Protocol):
    provider_id: str
    async def search(self, query: str, limit: int = 10) -> Sequence[dict[str, Any]]: ...


@runtime_checkable
class ModelRegistryAdapter(Protocol):
    provider_id: str
    async def register(self, name: str, artifact_uri: str, *, metadata: dict[str, str]) -> str: ...
    async def resolve(self, name: str, alias: str = "champion") -> str | None: ...


@runtime_checkable
class TradingEngineAdapter(Protocol):
    provider_id: str
    async def backtest(self, request: dict[str, Any]) -> dict[str, Any]: ...
    async def reconcile(self, account_id: str) -> dict[str, Any]: ...


@runtime_checkable
class RetrievalIndexAdapter(Protocol):
    provider_id: str
    async def upsert(self, records: Sequence[dict[str, Any]]) -> None: ...
    async def query(self, query: str, *, limit: int = 10) -> Sequence[dict[str, Any]]: ...


@runtime_checkable
class SecurityScannerAdapter(Protocol):
    provider_id: str

    async def scan(self, target: str) -> Sequence[dict[str, Any]]: ...


@runtime_checkable
class ArtifactVerifier(Protocol):
    provider_id: str

    def verify(self, artifact: bytes, digest: str, signature: bytes | None = None) -> bool: ...


@runtime_checkable
@runtime_checkable
class EvaluationAdapter(Protocol):
    provider_id: str
    async def evaluate_drift(self, current_data: Any, reference_data: Any) -> dict[str, Any]: ...


@runtime_checkable
class ObservabilityAdapter(Protocol):
    provider_id: str
    def span(self, name: str, attributes: dict[str, Any] | None = None) -> Any: ...
    def metric(self, name: str, value: float, attributes: dict[str, Any] | None = None) -> None: ...


def require_adapter_capability(adapter: object, capability: type[Protocol]) -> None:
    """Fail closed when a configured integration does not implement its contract."""
    if not isinstance(adapter, capability):
        raise TypeError(
            f"adapter_missing_contract:{capability.__name__}:{type(adapter).__name__}"
        )
