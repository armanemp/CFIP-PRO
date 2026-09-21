import pytest

from cfip.domain.market_data_contracts import HistoricalMarketDataRequest
from cfip.infrastructure.providers.nautilus_adapter import NautilusBacktestAdapter, NautilusUnavailable
from cfip.infrastructure.providers.openbb_adapter import OpenBBMarketDataAdapter
from cfip.infrastructure.providers.oss_registry import capability_snapshot

def test_market_data_request_rejects_reversed_range() -> None:
    with pytest.raises(ValueError, match="end_epoch_must_follow_start_epoch"):
        HistoricalMarketDataRequest(symbol="EURUSD", timeframe="15m", start_epoch=20, end_epoch=10)

def test_openbb_rejects_unknown_timeframe_before_external_call() -> None:
    with pytest.raises(ValueError, match="unsupported_openbb_timeframe"):
        OpenBBMarketDataAdapter().fetch_historical(
            HistoricalMarketDataRequest(symbol="EURUSD", timeframe="7m")
        )

def test_nautilus_adapter_is_safe_when_package_is_absent_or_explicitly_unbound() -> None:
    adapter = NautilusBacktestAdapter()
    if not adapter.available():
        with pytest.raises(NautilusUnavailable):
            adapter.run(type("Request", (), {"strategy_name": "test"})())

def test_oss_registry_distinguishes_installed_from_operational() -> None:
    snapshot = capability_snapshot()
    assert snapshot
    assert all(item["configuration_state"] == "unknown" for item in snapshot)
    assert all(item["connection_state"] == "unknown" for item in snapshot)
    assert all(item["operational"] is False for item in snapshot)
