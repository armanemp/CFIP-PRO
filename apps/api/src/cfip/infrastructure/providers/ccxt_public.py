"""CCXT public market-data adapter.

The adapter is intentionally provider-only: exchange-specific objects never cross into
CFIP domain/application contracts. Credentials are not required for public OHLCV data.
"""

from __future__ import annotations

import ccxt.async_support as ccxt


async def fetch_public_ohlcv(
    exchange_id: str,
    symbol: str,
    timeframe: str = "1m",
    limit: int = 500,
) -> dict[str, object]:
    """Fetch normalized public OHLCV data from a CCXT-supported exchange."""
    if not exchange_id or not exchange_id.replace("_", "").isalnum():
        raise ValueError("invalid_exchange_id")
    if not symbol or len(symbol) > 64:
        raise ValueError("invalid_symbol")
    if limit < 1 or limit > 5000:
        raise ValueError("invalid_limit")

    exchange_type = getattr(ccxt, exchange_id, None)
    if exchange_type is None:
        raise ValueError("unsupported_exchange")

    exchange = exchange_type({"enableRateLimit": True})
    try:
        rows = await exchange.fetch_ohlcv(symbol, timeframe=timeframe, limit=limit)
        bars: list[dict[str, float | int]] = []
        for row in rows:
            if not isinstance(row, (list, tuple)) or len(row) < 6:
                continue
            values = [float(value) if value is not None else 0.0 for value in row[:6]]
            timestamp, open_, high, low, close, volume = values
            bars.append({
                "time": int(timestamp),
                "open": open_,
                "high": high,
                "low": low,
                "close": close,
                "volume": volume,
            })
        return {
            "provider": "ccxt",
            "exchange": exchange_id,
            "symbol": symbol,
            "timeframe": timeframe,
            "bars": bars,
        }
    finally:
        close = getattr(exchange, "close", None)
        if close is not None:
            await close()
