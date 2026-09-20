from __future__ import annotations

import pytest

from cfip.domain.oss_contracts import TradingEngineAdapter
from cfip.infrastructure.oss.nautilus_trader_engine import NautilusTraderEngineAdapter


def test_nautilus_adapter_implements_trading_contract() -> None:
    adapter = NautilusTraderEngineAdapter()
    assert isinstance(adapter, TradingEngineAdapter)
    assert adapter.provider_id == "nautilus-trader"


@pytest.mark.asyncio
async def test_nautilus_adapter_rejects_incomplete_request() -> None:
    adapter = NautilusTraderEngineAdapter()
    with pytest.raises(ValueError, match="data_required"):
        await adapter.backtest({})


@pytest.mark.asyncio
async def test_nautilus_adapter_reconcile_fails_closed() -> None:
    adapter = NautilusTraderEngineAdapter()
    with pytest.raises(NotImplementedError, match="live_reconciliation"):
        await adapter.reconcile("account-1")
