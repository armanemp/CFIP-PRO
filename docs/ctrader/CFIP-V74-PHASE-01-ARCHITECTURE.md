# CFIP-PRO cTrader v74 — Phase 1 Architecture Foundation

**Date:** 2026-09-27  
**Baseline:** `integrations/ctrader/calude-edit-v73.cs`  
**Implementation:** `integrations/ctrader/calude-edit-v74.cs`  
**Phase:** 1 — Architecture foundation and canonical contracts

## Purpose

v74 is the first clean architectural line after v73.

It deliberately does not copy the v73 monolith and then rename methods. v73 remains the behavioral/reference baseline. v74 establishes the contracts that later phases will populate.

## What was established

### 1. Canonical direction

`BUY=+1`, `SELL=-1`, `WAIT=0`.

### 2. Canonical entry model

The following terms are now represented by distinct types/properties:

- `IdealEntry`: preferred price inside the structural execution area.
- `EntryZone`: allowed structural retest region.
- `Trigger`: structural activation threshold.
- `RequestedEntry`: exact value requested from the broker.
- `ActualFill`: broker-confirmed result.
- `Invalidation`: structural/risk boundary.

No later implementation should collapse these meanings into one variable named Entry/Trigger/Level.

### 3. Plan / execution / broker separation

The code has separate contracts for:

`DecisionSnapshot -> TradePlan -> ExecutionIntent -> ExecutionResult -> BrokerStateSnapshot`

A requested execution value is never defined as an actual fill.

### 4. Lifecycle authority

`CFIPClean74LifecycleManager` is the only object that owns the lifecycle state field.

The host indicator does not assign lifecycle states directly.

The transition map is explicit and rejects unsupported jumps.

### 5. Broker mutation boundary

`ICFIPClean74BrokerGateway` is the only broker mutation contract exposed by the foundation.

v74 contains no direct cTrader order/position mutation calls.

The actual cTrader adapter is deferred to the execution phase.

### 6. Configuration/runtime separation

`CFIPClean74ConfigSnapshot` is a configuration value object.

`CFIPClean74RuntimeSnapshot` is runtime observation.

The phase establishes the separation before the public-parameter inventory and consolidation of Phase 2.

### 7. Execution envelope

The old broad reuse of one entry-extension limit is explicitly replaced in the contract by separate concepts:

- `MaxEntryChaseAtr`
- `MaxBrokerSlippagePips`
- `MaxBreakoutFillDeviationPips`
- `MaxPlanRebaseDistancePips`

### 8. Single target ladder

`CFIPClean74TargetLadder` owns TP1–TP4 as one ordered collection and validates directional monotonicity.

The effective broker target is a separate pointer/value in `ExecutionIntent`.

### 9. Provenance and fallback

Price/structure/target contracts carry `CFIPClean74Provenance`.

Fallbacks are typed through `CFIPClean74FallbackKind` instead of being indistinguishable from structural values.

### 10. Presentation boundary

A `CFIPClean74PresentationState` and `ICFIPClean74PresentationProjector` exist as downstream contracts.

The v74 host contains no chart drawing or trading UI.

## v73 migration mapping

| v73 shared concept | v74 owner |
| --- | --- |
| `_m5Frame` / MTF frame data | `MtfSnapshot` + `MarketModel` |
| `_decision` | `DecisionSnapshot` |
| `_executionModel` | `EntryModel` + `TradePlan` + execution planner |
| `_plan` | `TradePlan` + lifecycle/broker identity |
| broker stop/target fields | `BrokerStateSnapshot` |
| lifecycle field | `LifecycleManager` |
| target helper family | `TargetLadder` |
| normal/aggressive/pending gates | shared execution-policy contracts (later phase) |
| panel/chart authority | presentation projection only |

## Acceptance checks

Static architecture tests were added at:

`tests/unit/test_ctrader_v74_architecture.py`

They verify:

- direction contract
- entry terminology
- plan/intent/result/broker separation
- lifecycle authority
- execution idempotency identity
- target ladder directional validation
- separated execution constraints
- broker gateway boundary
- absence of chart authority
- service boundaries
- orchestration-shell shape
- balanced source braces
- absence of manual entry/trade UI

## Known limitations

Phase 1 is intentionally a compile-safe foundation shell, not the final trading indicator.

The following are deliberately deferred:

- public-parameter inventory and consolidation
- MTF resolver and closed-bar enforcement
- actual indicator/structure calculations
- decision implementation
- trade-plan construction
- risk/SL/target algorithms
- automatic market and pending execution
- broker adapters and event reconciliation
- live management
- outcome persistence
- panel/chart rendering
- performance/refactoring cleanup

A full cTrader runtime compile/test is still a later release gate; this phase relies on source-level architectural validation.

## Next phase

**Phase 2 — Configuration and parameter architecture**

The next phase will inventory the ~500+ v73 public parameters, assign every parameter a single semantic consumer, remove contradictory/duplicate controls, and produce one immutable effective configuration snapshot without reintroducing runtime/configuration coupling.
