# CFIP cTrader v60 Development Phases

Base: integrations/ctrader/calude-edit-v59.cs
Reference build remains unchanged. v60 is the active development branch.

## Phase 1 — Execution correctness
- Separate pre-trade plans from live-position management.
- Add TradingPermission status/request handling.
- Track position and pending-order lifecycle events.
- Recover managed positions after restart.

## Phase 2 — Risk and protection
- Spread-aware risk-free transition.
- Monotonic structural stop management.
- Momentum-aware stop tightening at earned R.
- Broker-side protection remains authoritative.

## Phase 3 — Adaptive reward
- Use one active broker take-profit.
- Keep TP1-TP4 as internal structural reward candidates.
- Ratchet the active TP forward only when a farther valid structural target is earned.

## Phase 4 — Automatic order intelligence
- Separate market auto-trading from automatic pending-order placement.
- Continuation mode: structure-confirmed stop orders.
- Reversal mode: structure-confirmed limit orders.
- Expiry and stale-direction cleanup.

## Phase 5 — Direction and reversal response
- Live reaction path evaluated on ticks.
- Pre-trade plans must not survive a decisive direction change.
- Profit-protection close requires strong opposite structure and evidence.
- Keep false/noisy flips from becoming trade churn.

## Phase 6 — Analysis expansion
- Add daily pivot context to target selection.
- Reuse existing MTF, FVG, OB, liquidity, structure, regime and momentum engines.
- Improve prediction/readiness presentation from decision + reaction + prediction state.

## Phase 7 — Runtime UI
- Separate AUTO TRADE and AUTO ORDERS quick controls.
- Show UTC/local session state and trading permission.
- Keep entry/SL/TP lines solid and labelled.
- Keep alerts, arrows and panel state synchronized.

## Phase 8 — Validation
- Compile in the installed cTrader environment.
- Test market order, stop order, limit order, position recovery, SL/TP modification, reversal close and end-of-day close on demo/backtest.
- Validate broker-specific minimum distances, volume steps, margin behavior and order expiry.
