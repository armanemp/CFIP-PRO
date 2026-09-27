# CFIP-PRO cTrader v82 — Phase 9 Execution Policy / Broker Gateway

Date: 2026-09-27

## Reference

- Parent implementation: `integrations/ctrader/calude-edit-v81.cs`
- New implementation: `integrations/ctrader/calude-edit-v82.cs`
- Phase 9 policy: `CFIPClean82ExecutionPolicy`
- Phase 9 planner: `CFIPClean82ExecutionPlanner`
- Phase 9 broker adapter: `CFIPClean82CTraderBrokerGateway`
- Static tests: `tests/unit/test_ctrader_v82_phase9_execution.py`

## Objective

Create one execution path for automatic market trading and automatic pending orders:

```
DecisionSnapshot
  -> EntrySnapshot
  -> TradePlan
  -> Execution Policy
  -> Risk/Sizing
  -> Broker Constraints / Margin
  -> Execution Intent
  -> Broker Gateway
  -> Execution Result
  -> Broker State Reconciliation
```

Market, continuation-stop and reversal-limit paths share the same policy gate. Their only execution difference is the broker order kind.

## Implemented

### Execution policy

`CFIPClean82ExecutionPolicy` now owns:

- exact Decision/Entry/Plan coherence
- Market vs automatic Pending eligibility
- Confirmed-signals gate
- automatic confidence / smart-quality / level-quality floors
- trading-enabled guard
- managed exposure limits
- daily realized-loss guard
- estimated-margin guard for the new order
- broker minimum SL/TP distance checks
- broker minimum/step volume checks
- configured risk-budget guard
- configured session-window and Friday cutoff guards
- explicit execution kind mapping
- typed execution block reasons

The new-order margin check uses cTrader's `Symbol.GetEstimatedMargin()` concept, rather than treating current `Account.Margin` as the cost of the new trade.

### Risk / sizing

The execution layer consumes the Phase-8 `TradePlan.ExecutionAnchor` and structural stop to calculate stop distance.

Risk-percent sizing uses cTrader's proportional-risk volume calculation. Fixed-lot sizing is normalized to the symbol's volume constraints.

The same execution-anchor distance is used again when calculating risk amount, preventing a Stop/Limit plan from being sized from a different price than the actual execution anchor.

The broker-facing policy also validates minimum SL/TP distances and volume constraints before the order reaches the gateway. These checks are derived from the runtime broker-constraint snapshot.

### Intent

`CFIPClean82ExecutionPlanner` converts the validated execution readiness into one `CFIPClean82ExecutionIntent`.

The intent carries:

- Signal identity
- Plan identity
- direction
- market/stop/limit kind
- requested entry
- trigger
- structural stop
- selected TP stage
- volume
- risk request
- decision policy mode
- pending expiry

Pending orders receive the combined signal/plan identity as a broker comment so later duplicate detection can reconcile persisted broker state.

### Broker gateway

All broker mutations live in `CFIPClean82CTraderBrokerGateway`.

Market execution records the broker-confirmed `ActualFill`. Pending orders record broker order identity and protection state. Protection modification, close and pending cancellation are also isolated behind the same gateway.

The gateway does not invent strategy levels: it receives stop/target values from the authoritative Plan/Intent.

For pending Stop/Limit orders, the SignalId/PlanId identity is persisted in the broker comment. cTrader's current Algo API exposes pending-order overloads with expiration and comment fields, which supports this reconciliation boundary.

### Lifecycle / reconciliation

After a valid readiness decision, the host advances the lifecycle through SignalDetected -> PlanReady -> ExecutionReady.

A successful market result advances to LivePosition. A successful pending result advances to PendingOrder. A rejected broker result becomes Rejected.

The broker state reader observes current managed positions and pending orders. It does not mutate broker state.

## Deep-audit notes

- The Phase-8 TradePlan now carries an explicit `ExecutionAnchor`; sizing and risk are based on that anchor instead of always using `IdealEntry`.
- New-order margin is evaluated from `GetEstimatedMargin` and current FreeMargin/projected margin.
- Broker minimum distance and volume constraints are checked before gateway mutation.
- Session/Friday entry guards are policy-owned rather than embedded inside broker calls.
- A requested Auto TP stage is never silently replaced by TP1; an unavailable stage blocks intent creation explicitly.
- Signal and Plan identities remain distinct.
- Pending-order broker comments contain the SignalId/PlanId pair, enabling durable duplicate detection across recalculations.
- Lifecycle mutation remains owned by the Lifecycle Manager; broker callbacks/state reads remain observational.

## API alignment

Current official cTrader Algo documentation exposes `GetEstimatedMargin()`, proportional-risk volume calculation, account margin/equity fields, and pending-order APIs with expiry/comment fields. The implementation uses those current contracts and still requires actual cTrader compilation against the installed platform API before acceptance.

## Validation

The v82 source-level tests cover:

- 513/513 parameter parity
- version isolation
- balanced braces
- single execution policy/planner/gateway
- exact Decision/Entry/Plan chain
- ExecutionAnchor sizing/risk
- estimated margin guard
- broker distance and volume constraints
- risk budget
- session/Friday gates
- distinct Auto Trading vs Automatic Orders gates
- pending-order identity comments
- broker mutation isolation
- broker-confirmed ActualFill/protection state
- idempotency
- lifecycle/reconciliation
- interface/implementation alignment
- absence of manual entry controls

No passing pytest run is claimed here. Real cTrader compilation and controlled broker scenarios remain mandatory acceptance gates.
