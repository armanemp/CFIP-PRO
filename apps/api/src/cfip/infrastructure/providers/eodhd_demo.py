"""EODHD demo adapter used only as a real external market-data test feed.

The adapter deliberately does not manufacture prices. Historical bars and the live
quote are fetched from EODHD's documented demo endpoints for EURUSD.FOREX.
"""

from __future__ import annotations

import asyncio
import json
import time
from datetime import UTC, datetime
from decimal import Decimal
from urllib.parse import urlencode
from urllib.request import Request, urlopen

BASE_URL = "https://eodhd.com/api"
DEMO_TOKEN = "demo"
DEMO_TICKER = "EURUSD.FOREX"


def _get_json(url: str) -> object:
    request = Request(url, headers={"User-Agent": "CFIP-PRO-demo-provider/0.1"})
    with urlopen(request, timeout=10) as response:  # noqa: S310 - fixed HTTPS EODHD endpoint
        return json.loads(response.read().decode("utf-8"))


async def fetch_demo_market(limit: int = 500) -> dict[str, object]:
    now = int(time.time())
    params = urlencode(
        {
            "api_token": DEMO_TOKEN,
            "interval": "1m",
            "from": now - 3 * 24 * 60 * 60,
            "to": now,
            "fmt": "json",
        }
    )
    history_url = f"{BASE_URL}/intraday/{DEMO_TICKER}?{params}"
    quote_url = f"{BASE_URL}/real-time/{DEMO_TICKER}?api_token={DEMO_TOKEN}&fmt=json"
    history_raw, quote_raw = await asyncio.gather(
        asyncio.to_thread(_get_json, history_url), asyncio.to_thread(_get_json, quote_url)
    )

    bars: list[dict[str, object]] = []
    if isinstance(history_raw, list):
        for row in history_raw[-limit:]:
            if not isinstance(row, dict) or row.get("datetime") is None:
                continue
            bars.append(
                {
                    "time": str(row["datetime"]),
                    "open": float(row["open"]),
                    "high": float(row["high"]),
                    "low": float(row["low"]),
                    "close": float(row["close"]),
                    "volume": float(row.get("volume") or 0),
                }
            )

    quote = quote_raw if isinstance(quote_raw, dict) else {}
    timestamp = quote.get("timestamp")
    if timestamp is None:
        observed_at = datetime.now(UTC)
    else:
        observed_at = datetime.fromtimestamp(int(timestamp), tz=UTC)
    bid = quote.get("bid")
    ask = quote.get("ask")
    close = quote.get("close")
    return {
        "provider": "eodhd-demo",
        "symbol": "EUR/USD",
        "ticker": DEMO_TICKER,
        "delay_note": "EODHD demo feed; quote availability and latency are provider-controlled.",
        "observed_at": observed_at.isoformat(),
        "bid": float(Decimal(str(bid))) if bid is not None else None,
        "ask": float(Decimal(str(ask))) if ask is not None else None,
        "last": float(Decimal(str(close))) if close is not None else None,
        "bars": bars,
    }
