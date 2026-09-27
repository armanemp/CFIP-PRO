# CFIP-PRO cTrader v77 — Phase 4 Market Model / Indicators / Regime

**Date:** 2026-09-27  
**Baseline:** `integrations/ctrader/calude-edit-v76.cs` + preserved v73 market-feature behavior  
**Implementation:** `integrations/ctrader/calude-edit-v77.cs`  
**Phase:** 4 — Market model: indicators, regime and confluence

## Objective

Build one normalized market model from the canonical v76 closed MTF snapshot.

The purpose is not to create a second signal engine. It is to produce one authoritative set of raw measurements and directional evidence that the later Decision Engine will consume.

## cTrader API basis

The current cTrader Algo API exposes `IIndicatorsAccessor` through the indicator runtime and supports EMA, RSI, ATR and Directional Movement System creation, including passing a specific `Bars` series for another timeframe. The DMS exposes ADX, DI+ and DI−. citeturn600001search0turn677072search1turn677072search4turn677072search2

## v73 behavior preserved

The following existing analytical concepts were carried forward with their core semantics:

- EMA fast/slow trend state
- EMA slope
- RSI midpoint bias and exhaustion-aware groundwork
- ADX
- DI+/DI− directional bias
- ATR volatility
- two-bar momentum normalized by ATR
- rejection wick/body behavior
- rolling tick-volume expansion
- EMA-difference MACD bias
- rolling VWAP bias
- healthy-volatility envelope
- ATR-ratio regime detection
- choppy-market detection

This is a migration of the analytical capability, not deletion of these features.

## New normalized contract

Each `CFIPClean77MarketFrame` contains:

### Raw measurements

- OHLC
- ATR and ATR ratio
- RSI
- ADX
- DMI bias
- EMA fast/slow
- EMA spread in ATR units
- EMA slope in ATR units
- momentum in ATR units
- MACD histogram proxy
- rolling VWAP
- volume ratio
- candle body/range in ATR units

### Typed interpretation

- `BiasDirection` — market bias only; **not** the final trade signal.
- `BiasStrength`
- normalized bull/bear scores
- independent evidence count
- enabled evidence feature count
- typed `Regime`
- `RegimeQuality`
- `Choppy`
- `HealthyVolatility`

### Evidence ledger

The model uses exactly one evidence record for each market feature:

1. Trend
2. Momentum
3. RSI
4. DMI
5. EMA Slope
6. Rejection
7. Volume Expansion
8. MACD Bias
9. VWAP Bias
10. Healthy Volatility

A feature can contribute directionally to one side only. Bull and bear versions of the same feature are not counted twice.

## Why this is important for Signal quality

The old v73 model exposed many booleans that downstream code could independently reuse and score again. v77 makes evidence explicit so the next Decision phase can distinguish:

- raw measurement
- interpretation
- directional evidence
- evidence weight
- gate eligibility
- presentation data

This prevents accidental double-counting when the Decision Engine combines MTF, Smart, Structure and Confluence evidence.

## Regime model

The v73 regime semantics are represented as a typed enum:

- TREND
- EXPANSION
- COMPRESSION
- RANGE
- TRANSITION
- UNKNOWN

ATR ratio and ADX remain the primary regime inputs, with EMA separation contributing transition/trend quality.

Regime is context, not an automatic BUY/SELL signal.

## MTF integrity

v77 never chooses a market-analysis index independently.

The Market Model receives:
- canonical v76 `MtfSnapshot`;
- its closed indices;
- closure proof metadata;
- one M5 reference timestamp.

The market frame builder rejects the last potentially forming bar and uses the snapshot's closed index.

## Capability preservation

No signal/trading capability was removed in Phase 4.

The following later phases remain responsible for their original and upgraded behavior:
- Structure/BOS/MSS/CHOCH
- FVG/OB
- Liquidity pools/sweeps
- final Signal/Decision
- Entry/Trigger
- smart SL
- unified TP ladder
- Automatic Market Trading
- Automatic Pending Orders
- broker protection
- live management
- partial TP/reversal
- outcome/calibration

The Market Model is intentionally upstream of all of them.

## Validation

Repository-side source validation confirmed:
- v76/v77 parameter surface remains 512/512 in identical order;
- native cTrader indicator access contracts are present;
- ten unique market features are explicitly registered;
- typed regime exists;
- market model is driven by closed MTF snapshots;
- forming-bar protection remains in place;
- no broker mutation/chart authority entered the market layer;
- version isolation and source brace balance are preserved.

The Python test file is included, but a direct pytest runner is not claimed here because the execution sandbox has previously lacked access to `raw.githubusercontent.com`. Final runtime proof remains Phase 16.

## Phase 4 acceptance

**COMPLETE at the architecture/market-model contract level.**

## Next phase

**Phase 5 — Structure, FVG, OB and Liquidity ledger.**
