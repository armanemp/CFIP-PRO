# CFIP-PRO Chart Provider Test Feed

## EODHD demo provider

CFIP-PRO now has an isolated `eodhd_demo` provider adapter and `/api/market/demo/eurusd` endpoint. It uses EODHD's documented `demo` token and the real `EURUSD.FOREX` instrument; it does not generate synthetic prices.

The provider supplies:

- recent 1-minute historical OHLCV bars when the upstream endpoint provides them;
- the current EUR/USD quote from the real-time endpoint;
- explicit provider/source metadata;
- a clear provider error rather than fabricated fallback prices.

EODHD documents `EURUSD.FOREX` as a demo instrument and documents a WebSocket forex feed with `EURUSD` available under the demo token. Its real-time REST endpoint is delayed and should not be treated as a production tick feed.

## CFIP boundary

The adapter lives under `apps/api/src/cfip/infrastructure/providers/`. The chart consumes the existing normalized market-observation shape. Replacing EODHD later with a broker/provider adapter does not require changing the chart renderer.

## Production direction

The next provider layer is a persistent WebSocket ingestion adapter that normalizes provider ticks into `MarketObservation`, persists them through the existing transaction/outbox boundary, and exposes a reconnecting stream to the chart. The demo adapter is deliberately isolated so it cannot become a hidden production dependency.
