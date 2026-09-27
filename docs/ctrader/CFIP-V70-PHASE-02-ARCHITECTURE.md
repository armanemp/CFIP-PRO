# CFIP-PRO cTrader v70 — Phase 2 Architecture & Dependency Map

Date: 2026-09-27
Repository: armanemp/CFIP-PRO
Branch: main
Source: integrations/ctrader/calude-edit-v70.cs
Current source blob: 4c1565435a70b6dd6782ad2fc0b3f3a14a429d96

## Purpose

This document records the actual dependency shape of v70 before introducing the next behavioral version.
v69 remains the frozen baseline. v70 is an isolated hardening line and is not intended to be the final modular architecture.

## Source inventory

- 24,981 source lines
- 787,196 characters
- 1 main cTrader Indicator class
- 513 public cTrader parameters
- 302 method-like declarations detected by static scanning
- One duplicate method name, AddScore, appears to be overload-related rather than a duplicate implementation
- 8 MTF series: M1, M5, M15, M30, H1, H4, D1, W1

## Current orchestration

The current Calculate() path remains a large orchestration boundary:

Market/MTF acquisition
-> closed-M5 selection
-> frame analysis
-> decision construction/filtering
-> suitability/early prediction
-> reaction
-> plan construction
-> active plan management
-> normal auto execution
-> aggressive auto execution
-> automatic pending execution
-> broker protection
-> outcome monitoring
-> reversal/session checks
-> broker synchronization
-> rendering

The v70 hardening work deliberately did not attempt a large behavioral rewrite inside this orchestration.

## Mutable state ownership map

The following fields are the highest-coupling state objects identified by source scanning:

| State | Approx. references | Main role | Required future owner |
| --- | ---: | --- | --- |
| _plan | 599 | signal/entry/live-plan state, targets, lifecycle linkage, rendering | PositionLifecycle + ExecutionPlan |
| _decision | 163 | direction, quality, evidence, filtering | DecisionEngine |
| _m5Frame | 192 | structure, liquidity, direction, M5 evidence | MtfAnalysis/Structure snapshot |
| _reaction | 72 | reaction/reversal confirmation | Reaction/Decision evidence model |
| _executionModel | 44 | entry/trigger/continuation/reversal execution mode | ExecutionModel |
| _activeBrokerStop | 13 | broker-held live stop snapshot | BrokerState/Protection |
| _activeBrokerTarget | 13 | broker-held live target snapshot | BrokerState/Target |
| _lifecycleState | 6 | explicit v70 lifecycle marker | PositionLifecycle |

These counts are static reference counts, not runtime mutation counts.

## High-coupling methods

The largest cross-domain methods remain:

- Calculate()
- EvaluateActivePlan()
- TryAutoTrade()
- TryAggressiveAutoTrade()
- ProtectBrokerPositions()
- RenderPanelRows()/panel composition
- the frame/decision construction pipeline
- plan construction and target enrichment/reselection helpers

The immediate architectural objective is not to split methods mechanically. The split must follow responsibility and side-effect ownership.

## Dependency rules for the final architecture

### Rule A — Market data is read-only input

Market/MTF acquisition must produce a snapshot with:
- symbol/time metadata
- closed-bar indices
- per-timeframe bars/frame state
- live bid/ask where required
- spread/volatility context

No market-data function may mutate plan, broker state or UI state.

### Rule B — Decision is pure policy output

Decision construction should consume snapshots and return:
- direction
- confidence/quality
- timeframe agreement
- evidence
- structural confirmation
- regime
- entry eligibility
- explicit block reasons

It must not place/cancel/close orders and must not draw chart objects.

### Rule C — Execution intent is distinct from execution result

The decision/plan layer produces an ExecutionIntent:
- side
- entry mode
- entry/trigger
- stop
- target set reference
- risk/volume request
- label/idempotency key
- policy mode

Broker execution returns a separate ExecutionResult:
- accepted/rejected
- order/position id
- actual fill price
- broker error
- protection status

No later stage may pretend the requested entry price is the actual fill price.

### Rule D — Broker state is authoritative after mutation

After any broker mutation:
- inspect TradeResult
- reconcile actual broker object
- update internal broker snapshot
- transition lifecycle only when the appropriate broker state exists

The v70 mutation gateways now establish this direction.

### Rule E — Lifecycle owns terminal state

Only broker lifecycle confirmation may finalize:
- live position closed
- pending order cancelled
- filled position adopted
- recovery requirement resolved

Telemetry timeout, UI cleanup or visual reset may not independently terminate broker ownership.

### Rule F — Presentation is downstream

Panel/chart rendering consumes a presentation snapshot.
Rendering may not:
- place orders
- cancel orders
- close positions
- decide eligibility

## Event model decision

v70 now subscribes to:

- Positions.Opened
- Positions.Modified
- Positions.Closed
- PendingOrders.Created
- PendingOrders.Modified
- PendingOrders.Filled
- PendingOrders.Cancelled

Handlers are deliberately observational/reconciliation-oriented. The full v76 lifecycle state machine should formalize event precedence versus polling/reconciliation.

Official cTrader documentation confirms the relevant modified-position and pending-order event types and handler signatures. citeturn507219search0turn507219search1turn507219search9

## Broker mutation authority map

Current direct broker mutation shape in v70:

- ExecuteMarketOrder: 2
- PlaceStopOrder: 1
- PlaceLimitOrder: 1
- ModifyStopLossPrice: 1
- ModifyTakeProfitPrice: 1
- ClosePosition: 4
- CancelPendingOrder: 1

The SL/TP price mutations are centralized in result-aware gateways.
Close/cancel operations are also routed through result-aware helpers for lifecycle-sensitive paths.

## Remaining Phase 2 risks

1. The Indicator class is still monolithic.
2. _plan remains referenced across many unrelated responsibilities.
3. Target selection still has more than one conceptual consumer and needs a single TargetSet contract.
4. Normal/aggressive/pending entry still have duplicated eligibility/safety logic.
5. Decision soft-bypass policy is not yet represented as an explicit named policy object.
6. Outcome persistence is not yet implemented.
7. Daily-loss baseline is not yet persistent/day-start authoritative.
8. EOD contract and manual-action UI policy are not yet normalized.
9. Full cTrader compilation/runtime validation is still pending.

## Phase 2 exit criteria

Phase 2 is considered structurally mapped when:
- market-data ownership is isolated
- decision ownership is isolated
- execution intent/result boundaries are explicit
- plan/lifecycle ownership is explicit
- broker mutation authority is centralized
- UI is prohibited from being an execution authority
- target/risk/protection contracts are named before they are extracted

v70 now satisfies the mapping/contract-definition portion. Mechanical service extraction begins from v71 onward.
