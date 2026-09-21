"""Optional OpenBB market-data adapter."""
from __future__ import annotations
from datetime import UTC, datetime
from cfip.domain.analysis import CandleInput
from cfip.domain.market_data_contracts import HistoricalMarketDataRequest, HistoricalMarketDataResult

class OpenBBUnavailable(RuntimeError):
    """Raised when OpenBB is unavailable."""

class OpenBBMarketDataAdapter:
    id = "openbb"
    version = "odp"

    def available(self) -> bool:
        try:
            import openbb  # noqa: F401
        except ImportError:
            return False
        return True

    def fetch_historical(self, request: HistoricalMarketDataRequest) -> HistoricalMarketDataResult:
        try:
            from openbb import obb
        except ImportError as exc:
            raise OpenBBUnavailable("OpenBB is not installed") from exc

        kwargs: dict[str, object] = {"symbol": request.symbol, "provider": "yfinance"}
        if request.start_epoch is not None:
            kwargs["start_date"] = datetime.fromtimestamp(request.start_epoch, tz=UTC).date().isoformat()
        if request.end_epoch is not None:
            kwargs["end_date"] = datetime.fromtimestamp(request.end_epoch, tz=UTC).date().isoformat()
        try:
            result = obb.equity.price.historical(**kwargs)
        except Exception as exc:
            raise RuntimeError("openbb_historical_request_failed") from exc

        candles: list[CandleInput] = []
        for row in result.results[: request.limit]:
            timestamp = getattr(row, "date", None) or getattr(row, "datetime", None)
            if timestamp is None:
                raise ValueError("openbb_result_missing_timestamp")
            epoch = int(timestamp.timestamp()) if hasattr(timestamp, "timestamp") else int(timestamp)
            candles.append(CandleInput(
                time=epoch,
                open=float(row.open),
                high=float(row.high),
                low=float(row.low),
                close=float(row.close),
                volume=float(getattr(row, "volume", 0) or 0),
            ))
        return HistoricalMarketDataResult(
            symbol=request.symbol,
            timeframe=request.timeframe,
            candles=candles,
            provider=self.id,
            provider_version=self.version,
            provenance="openbb:equity.price.historical:yfinance",
            fetched_at_epoch=int(datetime.now(UTC).timestamp()),
        )
