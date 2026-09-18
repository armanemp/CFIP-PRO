from __future__ import annotations

import pytest

from cfip.infrastructure.providers import ccxt_public


class FakeExchange:
    def __init__(self, _config: dict[str, object]) -> None:
        self.closed = False

    async def fetch_ohlcv(
        self, _symbol: str, *, timeframe: str, limit: int
    ) -> list[list[float | int | None]]:
        assert timeframe == "1m"
        assert limit == 3
        return [
            [2000, 1.2, 1.3, 1.1, 1.25, 10],
            [1000, 1.1, 1.2, 1.0, 1.15, 8],
            [1000, 1.1, 1.2, 1.0, 1.15, 8],
            [3000, 1.3, 1.4, 1.2, 1.35, 9],
            [4000, 1.3, 1.1, 1.2, 1.25, 4],
            [5000, 1.4, 1.5, 1.3, None, 4],
        ]

    async def close(self) -> None:
        self.closed = True


@pytest.mark.asyncio
async def test_ccxt_normalizes_sorted_deduplicated_valid_bars(
) -> None:
    exchange = FakeExchange({})
    result = await ccxt_public.fetch_public_ohlcv(
        "fakeexchange",
        "BTC/USDT",
        limit=3,
        exchange_factory=lambda _config: exchange,
    )

    assert [bar["time"] for bar in result["bars"]] == [1000, 2000, 3000]
    assert exchange.closed is True


@pytest.mark.parametrize(
    ("exchange_id", "symbol", "timeframe", "limit", "error"),
    [
        ("bad-id!", "BTC/USDT", "1m", 10, "invalid_exchange_id"),
        ("fakeexchange", "", "1m", 10, "invalid_symbol"),
        ("fakeexchange", "BTC/USDT", "", 10, "invalid_timeframe"),
        ("fakeexchange", "BTC/USDT", "1m", 0, "invalid_limit"),
    ],
)
async def test_ccxt_rejects_invalid_request(
    exchange_id: str,
    symbol: str,
    timeframe: str,
    limit: int,
    error: str,
) -> None:
    with pytest.raises(ValueError, match=error):
        await ccxt_public.fetch_public_ohlcv(
            exchange_id,
            symbol,
            timeframe,
            limit,
        )
