# CFIP-PRO cTrader v80 — Phase 7 Entry / Trigger / Retest / Breakout

Date: 2026-09-27

## Reference

- Parent implementation: `integrations/ctrader/calude-edit-v79.cs`
- New implementation: `integrations/ctrader/calude-edit-v80.cs`
- Phase 6 source contract: `CFIPClean80DecisionSnapshot`
- Phase 7 engine: `CFIPClean80EntryTriggerEngine`
- Static tests: `tests/unit/test_ctrader_v80_phase7_entry.py`

## Purpose

Remove the historical ambiguity between Entry, Ideal Entry and Trigger without giving the Entry engine any broker or UI authority.

Canonical chain:

```
Structural Zone
    ↓
Ideal Entry
    ↓
Retest Eligibility OR Breakout Trigger
    ↓
Trigger Reached
    ↓
Requested Entry
    ↓
Actual Fill (later broker/lifecycle phase)
```

## Ownership

Phase 6 remains the sole owner of final directional decision.

Phase 7 consumes the exact `DecisionSnapshot` instance produced by Phase 6 and does not recompute direction from MarketModel, StructureSnapshot or indicators.

Phase 7 owns:

- retest eligibility
- breakout trigger construction
- trigger reached state
- requested-entry construction
- entry invalidation
- stale/expiry handling
- spread-aware entry blocking
- M1/M5 confirmation gating
- entry-distance and late-entry limits
- continuation/reversal pending-mode selection as an execution-mode proposal only

Phase 7 does not own:

- broker mutation
- position/order lifecycle
- initial SL/TP generation
- final volume sizing
- broker fill confirmation
- UI/chart rendering

## Entry semantics

### Retest

A directional execution-eligible FVG/OB zone is selected using quality first, then distance.

Current-price eligibility uses an ATR-based zone tolerance. Optional close-confirmation and rejection-body requirements are applied from the closed M5 frame.

The resulting model contains:

- `IdealEntry` = zone midpoint
- `EntryZone` = current active structural zone
- `Trigger` = null for a pure retest-market entry
- `Invalidation` = structural zone boundary expanded by the configured invalidation ATR

`RequestedEntry` is the current executable bid/ask. It is not treated as `IdealEntry`.

### Breakout

The engine selects a recent direction-matching M5 BOS/MSS/CHOCH/Displacement event.

`Trigger` is derived from the structural event price plus/minus the configured precision breakout ATR buffer.

`TriggerReached` is computed against the executable bid/ask with the entry buffer tolerance.

After Trigger is reached, `RequestedEntry` is created from the live executable price.

Trigger is therefore explicitly distinct from RequestedEntry and from ActualFill.

## Safety gates

The Entry engine blocks on:

- unavailable/incoherent input
- missing M5 entry frame
- invalid executable price
- spread exceeding the configured ATR ratio
- M5 confirmation mismatch
- optional M1 trigger mismatch
- missing structural zone
- stale breakout setup
- setup invalidation
- excessive entry distance
- excessive breakout extension
- precision-entry quality floor

No blocked state is converted into a broker action.

## Versioning

v79 remains frozen.

v80 is an independently recoverable implementation line and retains the exact 513-parameter surface of v79.

No v79/v78 type identifiers are present in v80.

## Validation

Static repository checks currently confirm:

- v79/v80 parameter count: 513/513
- exact parameter-name/order parity
- balanced C# braces
- no duplicate C# type declarations
- no missing CFIPClean80 type declarations referenced by v80
- no CFIPClean79/78 contamination in v80
- exactly one host DecisionEngine evaluation
- exactly one host EntryEngine evaluation
- Entry receives `_state.Decision` directly
- Entry engine has no broker mutation calls
- Entry engine has no chart/UI authority
- retest, breakout, continuation-stop and reversal-limit modes are represented
- spread, M1/M5 confirmation, stale/expiry, invalidation and late-entry gates are represented
- 19 dedicated Phase 7 source-validation tests are committed

The GitHub workflow `.github/workflows/ctrader-static.yml` is configured to run the Phase 6 and Phase 7 Python source tests. No executed workflow result is claimed here because the available GitHub connector did not expose the push-triggered run in this session.

Real cTrader compilation and controlled broker execution remain mandatory final runtime gates.
