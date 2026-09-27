# CFIP-PRO cTrader v86 — Phase 11 Lifecycle Completion

Date: 2026-09-27

## Reference

- Parent: `integrations/ctrader/calude-edit-v85.cs`
- New source: `integrations/ctrader/calude-edit-v86.cs`
- Tests: `tests/unit/test_ctrader_v86_phase11_lifecycle.py`

## Purpose

v86 closes the remaining architectural gaps identified in the Phase 11 re-audit before Live Management.

## Implemented

### Close request/result/confirmation
A Position close is represented as an explicit lifecycle request. The broker gateway returns an execution result, but accepted submission does not itself mark the Position closed. The Position remains `ExitRequested` until cTrader's Closed event or broker-state reconciliation confirms disappearance.

### Protection retry/backoff
Missing or drifted Position protection becomes a queued gateway action with deterministic retry timing. Failures use bounded exponential backoff (2, 4, 8, 16, 32, then 60 seconds).

### Pending fill ownership
A consumed PendingOrder is no longer treated as the broker mutation target for Position protection recovery. The resulting Position lifecycle owns protection mutation using the actual Position id.

### Multi-position fill support
A PendingOrder lifecycle record can retain multiple broker Position ids for the same PlanId, supporting partial-fill / multi-position scenarios without overwriting the first Position identity.

### Restart-safe protection handoff
When a PendingOrder fill event was missed and startup reconciliation discovers resulting Positions, expected SL/TP are transferred from the PendingOrder lifecycle record to each associated Position record before Position recovery actions are evaluated.

### Broker disconnect safety
Broker reconciliation and pending/position mutation processors do not consume broker state or issue mutation actions while cTrader reports the broker connection as unavailable. Existing internal ownership is retained for later recovery.

### Time authority
The clean lifecycle line contains no `DateTime.Now` / `DateTime.UtcNow` calls. Platform-provided `TimeInUtc` / runtime timestamps remain authoritative.

## Important invariant

A successful close request is not a close confirmation.

## Remaining Phase 11 work

Only controlled runtime validation remains for the implemented lifecycle semantics, plus governed policy decisions around orphan remediation.

## Next phase

**Phase 12 — Live management: SL, TP, partials, reversal, exhaustion**

Phase 12 will consume the Position lifecycle instead of directly coupling strategy logic to cTrader broker objects.
