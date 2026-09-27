# CFIP cTrader — v88 Phase 12 Live Management Record

Date: 2026-09-27

## Status

IMPLEMENTED / STATIC VERIFICATION COMPLETE / RUNTIME ACCEPTANCE PENDING.

Parent implementation line: v87.

Current source:
- `integrations/ctrader/calude-edit-v88.cs`

Static validation:
- `tests/unit/test_ctrader_v88_phase12_live_management.py`
- CI workflow run `36322249439`: 206 passed.

## Scope implemented

The Live Position Manager owns in-trade management for the live Position lifecycle, including:

- break-even and risk-free protection;
- structural SL repricing;
- dynamic target advancement;
- partial TP and target-stage progression;
- reversal protection and live structural reversal;
- profit-exhaustion protection;
- setup invalidation handling;
- end-of-day auto-close behavior;
- broker-result-aware protection, partial and close state transitions.

## Critical redesign

A live Position now owns a `CFIPClean88LivePlanSnapshot` containing the original PlanId, direction, execution anchor, structural stop, invalidation and TP1-TP4 target levels.

When the current market setup rolls over to a different plan, live management can continue from the Position-owned snapshot instead of silently switching to the new signal or losing the original target ladder.

## Safety/ownership invariants

- Live mutations are issued through the broker gateway.
- Broker acceptance does not itself mean a Position is closed or a partial target is consumed.
- Protection and partial actions remain retry-aware and idempotent.
- Manual BUY/SELL/order-placement controls remain absent.
- Platform runtime time remains the source of truth.

## Remaining before Phase 12 acceptance

- Durable persistence of Position-owned live-plan snapshots across full indicator restart when broker metadata cannot reconstruct the complete target ladder.
- Controlled broker validation of partial-close volume normalization and rejection/retry.
- Dedicated replay/scenario validation for reversal, exhaustion, EOD and protection-conflict precedence.
- Final real cTrader compile and controlled execution tests.
