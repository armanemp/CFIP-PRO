"""Regression tests for CFIP's vendor-neutral OSS adapter boundaries."""

import pytest

from cfip.domain.oss_contracts import (
    MarketDataAdapter,
    ModelRegistryAdapter,
    SecurityScannerAdapter,
    ArtifactVerifier,
    require_adapter_capability,
)


class FakeMarketData:
    provider_id = "fake"

    async def candles(self, symbol: str, timeframe: str, limit: int):
        return []


class FakeModelRegistry:
    provider_id = "fake"

    async def register(self, name: str, artifact_uri: str, *, metadata: dict[str, str]):
        return "model-1"

    async def resolve(self, name: str, alias: str = "champion"):
        return None


def test_market_adapter_is_runtime_checkable() -> None:
    assert isinstance(FakeMarketData(), MarketDataAdapter)


def test_model_registry_adapter_is_runtime_checkable() -> None:
    assert isinstance(FakeModelRegistry(), ModelRegistryAdapter)


def test_missing_capability_fails_closed() -> None:
    with pytest.raises(TypeError, match="adapter_missing_contract"):
        require_adapter_capability(object(), MarketDataAdapter)


class FakeSecurityScanner:
    provider_id = "fake"

    async def scan(self, target: str):
        return []


class FakeArtifactVerifier:
    provider_id = "fake"

    def verify(self, artifact: bytes, digest: str, signature: bytes | None = None):
        return True


def test_security_scanner_and_artifact_verifier_contracts() -> None:
    assert isinstance(FakeSecurityScanner(), SecurityScannerAdapter)
    assert isinstance(FakeArtifactVerifier(), ArtifactVerifier)
