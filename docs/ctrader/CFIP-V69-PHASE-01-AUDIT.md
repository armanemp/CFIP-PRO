# CFIP-PRO cTrader v69 — Phase 0/1 Baseline Audit

Date: 2026-09-27
Repository: armanemp/CFIP-PRO
Baseline branch: main
Baseline commit: 1d08a4cd19b0fa349243ff3781044def8305ac78
Baseline file: integrations/ctrader/calude-edit-v69.cs
Baseline file blob: 81e0c2707331ae378a7a5708172fa3e9bfc23704

## Scope

This document freezes the current v69 implementation before architectural refactoring.
v69 itself must remain recoverable and must not be overwritten by later versions.

## Current source inventory

- ~24,126 source lines
- ~761 KB file size
- 1 main Indicator class
- 8 MTF Bars series: M1/M5/M15/M30/H1/H4/D1/W1
- 513 public cTrader parameters
- 9 runtime model types (including nested enum/helper types)
- 100+ meaningful methods
- Major monolithic regions:
  - Plan Engine
  - Unified Panel
  - Execution Coordination
  - Structure and Zones
  - Alert Engine
  - Active Plan Management
- Trade-side calls include market execution, pending stop/limit placement, close and cancellation.
- Broker lifecycle events currently subscribed: Positions.Opened, Positions.Closed, PendingOrders.Filled.

## Baseline execution pipeline

Calculate() currently orchestrates:
MTF acquisition
-> closed-M5 analysis
-> Decision
-> market suitability
-> early prediction
-> reaction
-> plan creation
-> active-plan management
-> normal auto trade
-> aggressive auto trade
-> automatic pending orders
-> broker protection
-> outcome monitoring
-> reversal/session checks
-> broker synchronization
-> rendering

This is functional orchestration, but too many responsibilities are coupled to mutable shared fields.

## Confirmed findings

### C01 — Live-plan lifecycle can be severed by telemetry timeout
Location: Evaluate/MonitorOutcome area (~215xx-22xxx region).

MonitorOutcome() can classify an unresolved live position after OutcomeMaximumM5Bars and then set _plan = null while the broker position remains open.
This creates an unsafe distinction between strategy telemetry completion and actual broker lifecycle.
Required final design: telemetry timeout must never terminate live-position ownership.

Severity: CRITICAL

### C02 — Structural invalidation can clear the plan without a broker exit
CheckStructuralSetupInvalidation() can register a loss, increment losses, clear _plan and remove plan objects without closing the corresponding broker position.
This leaves strategy state and broker state divergent unless another protection/recovery path happens to recover the position.
Required final design: separate PlanInvalidated, ExitRequested and PositionClosed states.

Severity: CRITICAL

### C03 — Pending fill recovery depends on both broker SL and TP being present
OnPendingOrderFilled() only constructs a managed live Plan when both StopLoss and TakeProfit are present.
A successfully filled managed order with partial/missing protection can therefore become a live broker position without a managed plan.
Required final design: every managed fill must enter a managed lifecycle state; missing protection becomes an explicit recovery/error state.

Severity: HIGH

### C04 — Partial TP hit-state is committed before close success is known
EvaluateActivePlan() marks _tp1Hit/_tp2Hit before ExecutePartialClose() reports whether the broker close succeeded.
ExecutePartialClose() is void and swallows operation failures.
Therefore a failed partial close can become a permanently consumed TP event with no retry.
Required final design: broker mutation returns explicit result; hit-state is distinct from partial-execution state.

Severity: HIGH

### C05 — Runtime authority and public parameters are still coupled
SetAutoTradingRuntimeState()/SetAutomaticOrdersRuntimeState() write both runtime flags and the public cTrader parameters.
At the same time EnsureExecutionRuntimeState() mirrors parameter values back into runtime flags.
This creates two-way state coupling and makes UI/config/runtime ownership ambiguous.
Required final design: immutable configuration snapshot + explicit runtime state authority.

Severity: HIGH

### C06 — Market, aggressive and pending execution paths duplicate policy logic
Normal market execution, aggressive execution, pending stop and pending limit each repeat parts of:
permission, safety, sizing, SL/TP construction, validation, position limits and suitability.
This is a major source of policy drift.
Required final design: one ExecutionPolicy/Eligibility pipeline with execution-mode-specific entry handling.

Severity: HIGH

### C07 — Target logic is distributed across multiple consumers
Structural target generation is spread across BuildTargetLevels, SelectTargets, SelectTarget, AutoTarget, SelectStructuralAutoTarget, FindFurtherLiveTarget, EnrichLivePlanTargets, UpdateUnhitTargetsLive and broker-protection logic.
Even where helper methods are already shared, each path can reselect or reinterpret target state.
Required final design: authoritative TargetSet + explicit effective broker target.

Severity: HIGH

### C08 — Decision/Plan/Execution objects are mutable shared state
_decision, _reaction, _prediction, _executionModel and _plan are mutable instance fields read and written from multiple lifecycle stages.
This makes same-cycle ordering part of correctness.
Required final design: immutable snapshots between stages wherever possible.

Severity: HIGH

### C09 — Decision filtering contains an explicit soft bypass
ShouldCreatePlan() can allow plan creation while Decision.EntryAllowed is false when allowUnconfirmedAutoPlan is true and confidence/quality thresholds are met.
This may be intentional, but it is currently encoded as a boolean bypass rather than an explicit execution policy/mode.
Required final design: named policy modes such as Confirmed, Soft, Aggressive and Pending.

Severity: MEDIUM

### C10 — Outcome/calibration state is session-memory only
_wins, _losses, _directionSamples and _directionWins are in-memory fields.
Restart/reload loses the calibration and outcome history.
Required final design: telemetry events must be persisted or explicitly treated as session-local observations.

Severity: MEDIUM

### C11 — Daily loss baseline is attachment-time state, not guaranteed day-start state
DailyLossLimitHit() initializes _dailyStartEquity the first time it runs for the current date.
If the indicator is attached/restarted during the day, the baseline is the current equity at attachment/restart time.
Required final design: explicit session/day baseline policy with persistence or a broker/account history source.

Severity: MEDIUM

### C12 — Existing managed pending orders are not automatically canceled when the daily loss limit is reached
The pending-order path blocks creation after DailyLossLimitHit(), but existing managed pending orders can remain in place.
Required final design: define whether daily loss circuit-breaker cancels all outstanding managed entry orders.

Severity: HIGH SAFETY REVIEW

### C13 — End-of-day behavior is contract-sensitive and must be normalized
The source contains EnableEndOfDayAutoClose with default true, while historical version notes describe the EOD behavior as alert-oriented and not automatically closing positions.
This is a contract inconsistency that must be resolved explicitly before finalization.

Severity: HIGH

### C14 — At least one declared parameter is currently unwired
ShowEarlyArrow appears to be declared but has no consumer in the current source scan.
Required final design: every parameter must be ACTIVE, DEPRECATED or REMOVED; no silently dead controls.

Severity: MEDIUM

### C15 — Manual safety/action controls remain in the source
The panel still contains CLOSE/CANCEL actions and ShowTradeActionButtons/AlwaysShowSafetyButtons.
These must be aligned with the current product requirement that trade entry/order placement be automatic and not exposed as manual trade-entry controls.

Severity: PRODUCT/UX CONTRACT

## Structural dependency map

Market Data
 -> Frame Analysis
 -> Decision
 -> Execution Model
 -> Plan
 -> Risk / Safety
 -> Broker Execution
 -> Broker Lifecycle
 -> Position Management
 -> Outcome

Cross-cutting today:
- _plan is referenced across decision, execution, protection, alerts and UI.
- _decision is referenced by both plan and pending execution.
- UI rendering reads operational state directly.
- alerts are emitted from domain logic instead of consuming domain events.

## Phase 1 acceptance gates

1. v69 remains unchanged and recoverable.
2. All cTrader API usage is verified against the target cTrader API/compiler.
3. No unresolved compile errors or API signature mismatches.
4. All public parameters have a known consumer or an explicit disposition.
5. All broker mutations have explicit success/failure handling.
6. Strategy lifecycle cannot become flat while a managed broker position remains live.
7. Pending fills cannot escape managed lifecycle ownership.
8. Normal/aggressive/pending execution use a single policy contract.
9. Structural target state is authoritative and distinguishable from broker-held target state.
10. Chart/panel state is presentation only and never an execution authority.

## Next implementation order

v70 Phase 1:
- preserve v69 unchanged
- establish explicit runtime/config separation
- harden live-position lifecycle ownership
- harden pending-fill recovery
- make partial TP mutations result-aware
- establish compile/API validation checklist

v71+:
- Decision contract
- Structure/Zone contract
- Entry/Execution contract
- Risk/SL
- Target/Reward
- unified execution
- lifecycle/protection
- telemetry/alerts
- UI/presentation
- scenario/replay matrix
- final modular refactor

## Progress

Phase 0 — Baseline: COMPLETE
Phase 1 — Static/API audit: IN PROGRESS
Phase 2 — Architecture extraction: STARTED
Code refactor: NOT STARTED
