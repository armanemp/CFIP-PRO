# CFIP cTrader — v87 Phase 11 Hardening Record

Date: 2026-09-27

## Status

IMPLEMENTED / RUNTIME VERIFICATION PENDING.

Parent implementation line: v86.

Current source:
- `integrations/ctrader/calude-edit-v87.cs`

Static validation:
- `tests/unit/test_ctrader_v87_phase11_hardening.py`
- CI workflow run `36322249439`: 206 passed.

## Corrections completed

- Repaired missing multi-position Position-id storage on PendingOrder records.
- Added explicit pending cancel/protection action state, result feedback and bounded retry/backoff.
- Prevented broker-event visibility lag from immediately reconciling an accepted pending order as absent.
- Made Position SL/TP verification detect both missing protection and price drift.
- Centralized final Stop/Target direction and broker-distance checks in the broker gateway mutation boundary.
- Changed exposure gating to count managed Positions plus Pending Orders together.
- Required SignalId/PlanId execution identity.
- Added restart/recovery lifecycle adoption paths for broker-existing managed Positions and Pending Orders.
- Added broker-confirmation grace around accepted market/pending execution to prevent false close/recovery transitions.
- Made event handlers null-safe around runtime state.
- Removed the obsolete manual trade-action parameter; no manual BUY/SELL/order-placement authority exists.
- Removed system-clock calls from lifecycle logic; platform time is the time authority.

## Acceptance still pending

Real cTrader compilation and controlled broker scenarios are mandatory, especially multi-position fills, partial fills, reconnect/recovery, broker rejection/retry, rate limits and restart with live broker state.
