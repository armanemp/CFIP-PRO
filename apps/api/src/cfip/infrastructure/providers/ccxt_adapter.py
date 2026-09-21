"""Optional CCXT market-data adapter for exchange/crypto connectivity."""
from __future__ import annotations
from datetime import datetime, timezone
from cfip.domain.analysis import CandleInput
from cfip.domain.market_data_contracts import HistoricalMarketDataRequest, HistoricalMarketDataResult

class CCXTUnavailable(RuntimeError):
    pass

class CCXTMarketDataAdapter:
    id = "ccxt"
    def __init__(self, exchange_id: str = "binance") -> None:
        self.exchange_id = exchange_id
    @property
    def version(self) -> str:
        try:
            import ccxt  # type: ignore
            return str(ccxt.__version__)
        except ImportError:
            return "unavailable"
    def available(self) -> bool:
        try:
            import ccxt  # type: ignore
            return hasattr(ccxt, self.exchange_id)
        except ImportError:
            return False
    def fetch_historical(self, request: HistoricalMarketDataRequest) -> HistoricalMarketDataResult:
        try:
            import ccxt  # type: ignore
        except ImportError as exc:
            raise CCXTUnavailable("ccxt_not_installed") from exc
        exchange_cls = getattr(ccxt, self.exchange_id, None)
        if exchange_cls is None:
            raise ValueError("unsupported_ccxt_exchange")
        exchange = exchange_cls({"enableRateLimit": True})
        try:
            if not exchange.has.get("fetchOHLCV"):
                raise RuntimeError("ccxt_ohlcv_not_supported")
            rows = exchange.fetch_ohlcv(request.symbol, timeframe=request.timeframe, limit=request.limit)
        finally:
            close = getattr(exchange, "close", None)
            if close is not None:
                import asyncio
                result = close()
                if hasattr(result, "__await__"):
                    try:
                        asyncio.run(result)
                    except RuntimeError:
                        pass
        candles = [CandleInput(timestamp=int(row[0] // 1000), open=float(row[1]), high=float(row[2]), low=float(row[3]), close=float(row[4]), volume=float(row[5])) for row in rows]
        return HistoricalMarketDataResult(
            symbol=request.symbol,
            timeframe=request.timeframe,
            candles=candles,
            provider=f"ccxt:{self.exchange_id}",
            provider_version=self.version,
            provenance=f"ccxt/{self.exchange_id}/fetch_ohlcv",
            fetched_at_epoch=int(datetime.now(timezone.utc).timestamp()),
        )
