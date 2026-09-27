# CFIP cTrader — v89 Phase 12 Live-Management Re-audit

Date: 2026-09-27

## Status

IMPLEMENTED / SOURCE-LEVEL RE-AUDIT PASS / REAL cTRADER RUNTIME ACCEPTANCE PENDING.

Parent implementation line: v88.

Current source:
- `integrations/ctrader/calude-edit-v89.cs`

Static re-audit:
- `tests/unit/test_ctrader_v89_phase12_reaudit.py`

## Why v89 exists

v88 completed the Phase 12 live-management implementation, but a second architecture-level review found synchronization edge cases that were not covered by the first Phase 12 gate.

v89 preserves the v88 public parameter surface and changes only lifecycle synchronization / recovery behavior.

## Corrections

### 1. Protection-action queue replacement

When a new live SL/TP request replaces a previously queued protection mutation, the old queue entry is removed and the Position record's pending-action state is explicitly reset.

The request now reports whether a replacement protection action was actually queued.

This prevents a stale `ProtectionActionPending` flag from silently suppressing the new mutation.

### 2. Plan-owned protection restoration

When a managed Position belongs to the current PlanId but the broker snapshot is missing SL and/or TP, live management can restore the missing protection from the authoritative current Plan:

- structural Stop from the Plan;
- managed target from the Position's active target stage.

Broker-side direction/distance validation remains mandatory at the gateway boundary.

### 3. Partial-close confirmation recovery

An accepted partial-close request that is not reflected in broker Position volume within the existing confirmation window no longer leaves `PartialPending` permanently latched.

The context enters an explicit recovery/retry path with bounded backoff.

TP1/TP2 consumption still occurs only after broker volume confirmation.

### 4. Close confirmation pacing

Accepted Position-close requests retain the `ExitRequested` state until broker Closed-event/reconciliation confirmation.

The retry interval after accepted submission is increased to 5 seconds to reduce repeated close mutation while broker state propagates.

### 5. Live protection validation anchor

Live Position protection mutation no longer reuses initial-entry validation semantics.

- BUY SL is validated against the current Bid-side execution boundary;
- SELL SL is validated against the current Ask-side execution boundary;
- BUY TP is validated against the current Ask-side execution boundary;
- SELL TP is validated against the current Bid-side execution boundary;
- broker minimum stop/TP distances remain enforced.

This removes the architectural contradiction where a valid break-even/risk-free/trailing stop above BUY entry (or below SELL entry) could be rejected by the gateway.

### 6. Restart adoption of unprotected managed Positions

Managed Positions with a missing broker SL are no longer discarded from the LivePositionManager solely because their reconstructed initial risk is zero.

A zero-risk reconstruction remains explicitly represented so matching current plans can still restore protection and lifecycle reconciliation can observe the broker object.

### 7. Broker-gateway ownership hardening

Position and pending-order mutation methods now independently require:

- managed strategy label;
- current symbol;
- CFIP89 identity marker.

This defense-in-depth check prevents accidental mutation of an unrelated broker object even if an invalid broker identifier reaches the gateway.

### 8. Initialization ordering

Engine state and lifecycle authority are now initialized before broker event subscriptions and existing-position adoption.

This removes a transient null-authority window during initialization.

## Invariants preserved

- No manual BUY/SELL/STOP/LIMIT entry controls.
- Broker mutations remain broker-gateway-owned.
- Position close is never marked closed merely because submission was accepted.
- Partial TP is never marked consumed before broker volume confirmation.
- Live management follows the Position-owned PlanId rather than silently adopting a new current signal.
- v88 remains preserved as the parent frozen implementation line.

## Validation

Source-level checks performed on v89:

- C# brace balance: PASS.
- v88 type/identity leakage: PASS.
- Main Indicator dependency declaration audit: PASS.
- Public parameter-surface parity with v88: PASS.
- Multi-position PendingOrder Position-id storage: PASS.
- Protection queue replacement synchronization: PASS.
- Plan-owned missing-protection restoration path: PASS.
- Partial-close timeout/retry path: PASS.
- Gateway final protection validation: initial/pending/live protection validation paths present.
- Live protection validation uses current market-side anchors: PASS.
- Managed broker-object ownership guards: PASS.
- Initialization ordering: PASS.
- Manual entry surface scan: PASS.

No claim of real cTrader compilation or live broker execution is made here.

## Remaining Phase 12 acceptance work

- durable persistence of Position-owned live-plan snapshots across full indicator restart when broker metadata is insufficient;
- real cTrader compilation in the target cTrader environment;
- controlled partial-close/rejection/retry scenarios;
- broker disconnect/reconnect validation;
- reversal/exhaustion/invalidation/EOD precedence scenarios;
- live event-ordering and rate-limit validation.

## Next architectural gate

Phase 13 — Outcome, telemetry, calibration and feedback — must consume authoritative lifecycle outcomes without becoming an execution authority.
