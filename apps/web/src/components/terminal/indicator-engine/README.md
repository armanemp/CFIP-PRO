# CFIP Indicator Engine

This directory is the boundary for terminal indicators.

Goals:

- keep chart rendering separate from indicator calculation
- standardize indicator metadata
- support OSS-backed calculations through adapters
- keep CFIP market intelligence and confluence logic independent

Planned groups:

- trend: EMA, SMA, WMA
- momentum: RSI, MACD, Stochastic
- volatility: ATR, Bollinger, Keltner, Donchian
- structure: FVG, order blocks, liquidity, market structure

The active terminal currently uses `chart-math.ts` and this boundary is being introduced incrementally without breaking existing behavior.
