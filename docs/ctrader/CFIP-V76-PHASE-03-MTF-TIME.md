# CFIP-PRO cTrader v76 — Phase 3 MTF / Time / Data Pipeline

**Date:** 2026-09-27  
**Baseline:** `integrations/ctrader/calude-edit-v75.cs` + v71 MTF/time contract  
**Implementation:** `integrations/ctrader/calude-edit-v76.cs`  
**Phase:** 3 — Time, MTF and data pipeline

## Objective

Turn the successful v71 timing rules into a first-class snapshot contract that downstream signal, planning and automatic execution code must consume rather than rebuilding time/index decisions independently.

## Verified cTrader API basis

Current cTrader Algo documentation exposes `Bars.OpenTimes` and `TimeSeries.GetIndexByTime()` for cross-timeframe time mapping. cTrader also exposes `TimeInUtc` as a shortcut to server UTC time and `Application.UserTimeOffset` as the platform's UTC offset for user-facing local time. citeturn196412search0turn196412search2turn430315search0turn196412search3

## What changed

### 1. Canonical M5 decision reference

`CFIPClean76MtfSnapshotBuilder.ResolveM5Reference()` derives the analytical reference from the active M5 series open time.

The server UTC clock is retained separately for runtime freshness and session timing.

This prevents downstream logic from mixing "current server time" with "the bar that defines the current analytical cycle."

### 2. Closed-bar resolver

`ResolveClosedBar()` is the single closed-index resolver.

It:
- rejects missing/insufficient series;
- uses `Bars.OpenTimes.GetIndexByTime(reference)`;
- never returns the final potentially-forming series item;
- requires the selected bar's next bar open to be at or before the M5 reference;
- records the selected bar open/next-open timestamps;
- records minimum-history readiness.

This is stronger than simply subtracting one from `Count`, especially around timeframe boundaries and sparse/missing intervals.

### 3. Immutable MTF snapshot contract

`CFIPClean76MtfSnapshot` carries:
- server UTC;
- M5 analytical reference UTC;
- user local time;
- reference age/freshness;
- chart closed snapshot;
- M1/M5/M15/M30/H1/H4/D1/W1 closed snapshots;
- typed data-status;
- primary decision readiness.

Each per-timeframe snapshot carries the closed index and proof metadata required by downstream consumers.

### 4. Temporal coherence check

`CFIPClean76MtfSnapshotBuilder.IsCoherent()` rejects a snapshot if any required timeframe:
- is unavailable;
- is not marked fully closed;
- has a negative index;
- opens at/after the reference;
- has its next bar open after the reference;
- or the reference is stale/invalid.

This gives later Signal/Decision code one authoritative temporal gate.

### 5. User time

The snapshot computes user-facing local time as:

`serverUtc + Application.UserTimeOffset`

The current cTrader documentation confirms that `Application.UserTimeOffset` represents the platform user's configured offset from UTC. citeturn196412search0turn196412search3

### 6. Eight MTF series

v76 explicitly acquires:
- M1
- M5
- M15
- M30
- H1
- H4
- D1
- W1

plus the host chart series for closed chart mapping.

### 7. No behavior change yet

Phase 3 intentionally does not implement Signal, Entry, Auto Trade or Auto Orders. It creates the data contract they must consume.

This prevents the time layer from becoming coupled to trading-side effects.

## Important carry-forward requirements for trading

The next signal/execution phases must never:
- re-resolve MTF indexes independently;
- use `Count - 1` as an analysis bar;
- substitute the host chart's live index for the M5 decision reference;
- mix server time with analytical reference time;
- treat a forming higher-timeframe bar as evidence;
- bypass `IsCoherent()` for convenience.

The live execution path may use current bid/ask, but only as a separate execution-time input. It must never rewrite the closed analytical snapshot.

## Validation performed

Repository-side validation confirmed:
- v75 and v76 parameter surfaces remain 513/513 and identical;
- v76 contains a single explicit MTF snapshot builder in the calculation cycle;
- all eight MTF series are present;
- the resolver rejects the final potentially-forming bar;
- selected bars require next-open <= reference;
- server UTC and user local time are separate;
- no local process clock is used;
- no direct broker mutations or chart authority were introduced;
- source braces are balanced.

### Validation limitation

These are repository/source-level checks. A real cTrader compile/runtime session is still required by Phase 16 and is not claimed here.


### Final review corrections

Two edge cases were corrected before Phase 3 acceptance:

1. `ResolveClosedBar()` now explicitly rejects a reference earlier than the first available bar, preventing an invalid historical fallback.
2. `IsCoherent()` now requires the primary decision timeframes to be ready/coherent, while treating unavailable optional D1/W1 history as missing data rather than temporal leakage.

The v76 source was re-fetched after these corrections and the parameter surface, source balance, timeframe coverage, closed-bar guards, UTC/local time separation, and absence of broker/UI side effects were rechecked.

### Test-run limitation

The repository test file was created and source-validated, but the execution sandbox could not resolve `raw.githubusercontent.com`; therefore an external `pytest` run could not be completed in this environment. This is an environment limitation, not a claim that the Python test runner passed.

## Phase 3 acceptance

**COMPLETE at the architecture/data-contract level.**

## Next phase

**Phase 4 — Market model: indicators, regime and confluence.**
