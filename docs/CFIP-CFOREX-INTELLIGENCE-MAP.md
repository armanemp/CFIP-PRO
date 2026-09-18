# CFIP-PRO — CForex Intelligence Parity Map

Status: active implementation reference
Date: 2026-09-18

## Source boundary

CForex is the capability/reference source. CFIP-PRO remains an independent implementation. The abandoned cforex-platform repository is not a migration target or architecture baseline.

## Verified CForex intelligence surface

The retained CForex source artifacts show a layered trading decision system rather than a collection of isolated indicators.

### Market/technical context

The CForex M15 entry engine uses, per timeframe:
- EMA 20 / 50 / 200
- ATR 14
- RSI 14
- DMI
- MACD 26/12/9
- Bollinger Bands 20 / 2
- Keltner Channels
- Donchian Channel
- Ichimoku

### Structure and SMC/price-action layer

The source explicitly models:
- confirmed swing highs/lows
- HH / HL / LH / LL
- BOS
- CHoCH
- equal-high / equal-low liquidity
- liquidity sweeps
- FVG
- order/zone context
- displacement
- premium/discount
- MTF alignment

### Decision/confluence layer

The CForex M15 engine exposes configurable confluence gates:
- minimum confluence score
- minimum bars between signals
- HTF alignment
- minimum aligned HTFs
- equal-liquidity usage and ATR-relative tolerance
- displacement OR structure-break requirement
- liquidity OR FVG requirement
- zone OR premium/discount requirement

The implementation also keeps explicit bull/bear evidence, raw/possible score, accepted state, trigger state, and a human-readable bias summary.

### Risk/target layer

The source calculates:
- entry
- stop
- risk in pips
- TP1 / TP2 / TP3
- R:R for each target
- proportional risk volume
- minimum stop distance
- ATR stop buffer
- M15 and HTF target lookbacks

Risk calculations are account/broker dependent and must not be fabricated in the CFIP terminal without account context.

### Realtime and signal lifecycle

The source emphasizes:
- closed-bar evaluation
- no higher-timeframe look-ahead
- minimum signal spacing
- startup alert suppression
- alert cooldown
- popup/sound/email notifications
- indicator-only execution boundaries

CFIP-PRO will preserve these semantics at the analysis/decision boundary.

## CFIP-PRO contract architecture

The frontend analysis contract is now centralized in:
apps/web/src/components/terminal/analysis-contracts.ts

The contract separates:
1. evidence-producing modules
2. normalized analysis context
3. confluence gates
4. one unified decision result

The unified result is the only terminal-level decision surface. Individual modules do not independently overwrite the final terminal answer.

Current implemented frontend modules:
- trend / structure
- momentum
- volatility / displacement
- volume
- FVG / zones
- liquidity pools and sweeps
- order blocks
- premium/discount
- MTF structure
- unified confluence/decision aggregation

## UI/UX contract

The terminal is chart-first and keeps the chart geometry stable across locale changes. Locale selection is scoped to the terminal/chart surface; it does not mutate the surrounding application document direction.

Terminal sizing/theme tokens are isolated in:
apps/web/src/components/terminal/terminal-theme.module.css

This file is the maintenance point for terminal widths, rail size, top bar/footer sizing, and shared terminal palette tokens.

## Next parity work

The next implementation layers are required parity work:
- DMI/ADX, Keltner, Donchian and Ichimoku as first-class analysis inputs
- causal/event-time MTF context with closed-bar semantics
- FVG lifecycle/mitigation/invalidation
- order-block mitigation/invalidation and breaker context
- true liquidity state and nearest-liquidity targets
- CForex-style entry/SL/TP/risk contract
- replay/backtest integration
- outcome attribution/calibration/drift
- notification/event lifecycle
- research/evidence grounding
- backend canonical analysis contract so frontend and backend cannot diverge
