# CFIP-PRO cTrader v85 — Phase 11 Re-audit and Hardening

Date: 2026-09-27

## Reference

- Parent implementation: `integrations/ctrader/calude-edit-v84.cs`
- New implementation: `integrations/ctrader/calude-edit-v85.cs`
- Phase authority carried forward: Decision -> Entry -> TradePlan -> Execution -> Broker -> Pending/Position lifecycle
- Static tests: `tests/unit/test_ctrader_v85_phase11_reaudit.py`

## Reason for v85

v84 was source-reviewed again before advancing to Live Management. The review found release-blocking defects and broker-ownership gaps that the existing Phase 11 tests did not detect.

v84 remains historical and is not overwritten.

## Corrections

### 1. Gateway contract alignment

The host called a non-existent `ModifyPositionProtection` gateway method while the interface and implementation expose `ModifyProtection`. v85 aligns the host call with the authoritative interface.

### 2. Pending-fill identity

Pending lifecycle called `MarkFilled(utc)` even though the record requires both fill time and broker Position identity. v85 records the actual Position id supplied by the Filled event.

### 3. Position expected-protection mutation

Position expected SL/TP properties used private setters but were assigned by the lifecycle manager. v85 moves this mutation behind a method on the Position record.

### 4. Version isolation

Structure records still emitted `CFIP79|` identities. v85 standardizes broker comment/identity/record markers on `CFIP85|` and keeps the source isolated from earlier clean versions.

The default auto-trade label also moves from the stale `CFIP-SMART-CLEAN66` value to `CFIP-SMART-CLEAN85`.

### 5. Broker-state ownership scope

Broker state counts and state-reader adoption were label-only. Reused labels could therefore admit broker objects from an older CFIP version. v85 requires the current `CFIP85|` identity marker for Position/Pending adoption, exposure counts, duplicate scans and daily realized-profit reconstruction.

### 6. Target integrity

Target ladder validation is strengthened to reject null levels, invalid stage sequence, non-finite prices, targets on the wrong side of the execution anchor, and non-monotonic stages.

### 7. Pending-order preflight

Automatic pending orders now reject expired plans and invalid live-side semantics before broker mutation.

### 8. Final gateway protection

The broker gateway now performs final directional validation of Stop Loss and Take Profit, so a malformed Intent cannot cross the mutation boundary.

## Validation boundary

The v85 static suite is intentionally source-level. It checks the contracts that were missed by the previous Phase 11 test set.

No real cTrader compilation or live broker scenario is claimed by this document. That remains mandatory before release acceptance.

## Remaining Phase 11 work

- deterministic close request/result/retry state machine
- partial-fill / multiple-position ownership
- explicit protection retry/backoff
- broker disconnect/recovery handling
- governed orphan remediation policy
- controlled cTrader runtime validation

## Next phase

**Phase 12 — Live management: SL, TP, partials, reversal, exhaustion**

Phase 12 will consume the now-authoritative broker Position lifecycle rather than directly mutating broker state from the analytical or presentation layers.
