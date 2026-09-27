# CFIP-PRO cTrader v83 — Phase 10 Pending Order Lifecycle

Date: 2026-09-27

## Reference

- Parent implementation: `integrations/ctrader/calude-edit-v82.cs`
- New implementation: `integrations/ctrader/calude-edit-v83.cs`
- Phase 10 manager: `CFIPClean83PendingOrderLifecycleManager`
- Phase 9 gateway extended with pending protection mutation
- Static tests: `tests/unit/test_ctrader_v83_phase10_pending.py`

## Objective

Turn automatic pending orders into a first-class lifecycle rather than leaving them as a side branch of initial execution.

Current cTrader Algo documentation exposes pending-order Created, Modified, Filled and Cancelled events. A Filled event provides both the pending order and the resulting position, while Cancelled provides the cancellation reason. The current API also exposes pending-order expiration, comments, and direct price-based Stop Loss / Take Profit modification methods. citeturn835745view0turn807792search0

## Implemented

### Lifecycle record

`CFIPClean83PendingOrderRecord` tracks:

- broker order identity
- SignalId / PlanId parsed from the persisted broker comment
- direction and execution kind
- requested target price
- expected structural stop / target
- creation and expected expiry time
- filled/cancelled timestamps
- lifecycle state

### Event contract

The host subscribes to:

- `PendingOrders.Created`
- `PendingOrders.Modified`
- `PendingOrders.Filled`
- `PendingOrders.Cancelled`

The event handlers remain observational: they update the lifecycle ledger and, for Filled, record the broker-created Position. They do not call broker mutation methods directly. Broker-changing actions are queued and later executed through the single broker gateway. citeturn835745view0

### Protection recovery

A submitted or modified pending order missing SL/TP becomes explicit recovery state. After a pending order fills, if the resulting Position is missing either protection component and the expected protection is known, a RestoreProtection action is queued.

The broker gateway implements pending-order price-based protection mutation through cTrader's current PendingOrder methods. citeturn557778view0

### Automatic cleanup

The lifecycle manager can queue:

- expiry cancellation
- daily-loss cancellation
- protection recovery

The host executes those actions only through `ICFIPClean83BrokerGateway`.

### Startup adoption / identity

Existing CFIP-managed pending orders are adopted at initialization. Broker comments are parsed into the SignalId / PlanId pair so restart reconciliation does not require volatile in-memory state.

## Invariants

- Pending order and live position are separate lifecycle states.
- Filled is confirmed from the cTrader Filled event and resulting Position.
- Cancelled is reconciled against actual remaining managed broker state rather than blindly forcing a Closed state.
- Broker mutation remains centralized in the gateway.
- No manual BUY/SELL/STOP/LIMIT controls are introduced.

## Limitations before acceptance

- stale-order invalidation beyond explicit broker expiry and daily-loss cancellation still needs a dedicated policy for setup supersession.
- partial-fill / multi-position semantics require scenario coverage in the runtime validation stage.
- restart adoption is source-modeled but still requires controlled cTrader runtime verification.
- Phase 9 execution-envelope / fill-deviation / plan-rebase policies remain pending.
- real cTrader compilation/runtime remains mandatory.
