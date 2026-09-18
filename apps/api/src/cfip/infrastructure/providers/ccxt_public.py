"""CCXT public market-data adapter.

The adapter is intentionally provider-only: exchange-specific objects never cross into
CFIP domain/application contracts. Credentials are not required for public OHLCV data.
"""

from __future__ import annotations

from collections.abc import Callable
from math import isfinite

import ccxt.async_support as ccxt


def _normalize_rows(rows: object) -> list[dict[str, float | int]]:
    if not isinstance(rows, list):
        raise ValueError("invalid_provider_payload")

    bars_by_time: dict[int, dict[str, float | int]] = {}
    for row in rows:
        if not isinstance(row, (list, tuple)) or len(row) < 6:
            continue
        try:
            timestamp, open_, high, low, close, volume = (
                float(value) for value in row[:6]
            )
        except (TypeError, ValueError):
            continue
        if not all(
            isfinite(value)
            for value in (timestamp, open_, high, low, close, volume)
        ):
            continue
        if timestamp <= 0 or min(open_, high, low, close) <= 0 or volume < 0:
            continue
        if high < max(open_, close, low) or low > min(open_, close, high):
            continue
        bars_by_time[int(timestamp)] = {
            "time": int(timestamp),
            "open": open_,
            "high": high,
            "low": low,
            "close": close,
            "volume": volume,
        }
    return [bars_by_time[key] for key in sorted(bars_by_time)]


async def fetch_public_ohlcv(
    exchange_id: str,
    symbol: str,
    timeframe: str = "1m",
    limit: int = 500,
    exchange_factory: Callable[[dict[str, object]], object] | None = None,
) -> dict[str, object]:
    """Fetch normalized public OHLCV data from a CCXT-supported exchange."""
    if not exchange_id or not exchange_id.replace("_", "").isalnum():
        raise ValueError("invalid_exchange_id")
    if not symbol or len(symbol) > 64:
        raise ValueError("invalid_symbol")
    if not timeframe or len(timeframe) > 16:
        raise ValueError("invalid_timeframe")
    if limit < 1 or limit > 5000:
        raise ValueError("invalid_limit")

    exchange_type = exchange_factory or getattr(ccxt, exchange_id, None)
    if not callable(exchange_type):
        raise ValueError("unsupported_exchange")

    exchange = exchange_type({"enableRateLimit": True})
    try:
        rows = await exchange.fetch_ohlcv(symbol, timeframe=timeframe, limit=limit)
        return {
            "provider": "ccxt",
            "exchange": exchange_id,
            "symbol": symbol,
            "timeframe": timeframe,
            "bars": _normalize_rows(rows),
        }
    finally:
        close = getattr(exchange, "close", None)
        if callable(close):
            await close()
