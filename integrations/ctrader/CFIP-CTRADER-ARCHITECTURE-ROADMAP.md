# CFIP-PRO cTrader — Clean Architecture Master Roadmap

**Document:** `integrations/ctrader/CFIP-CTRADER-ARCHITECTURE-ROADMAP.md`  
**Repository:** `armanemp/CFIP-PRO`  
**Baseline source:** `integrations/ctrader/calude-edit-v73.cs`  
**Created:** 2026-09-27  
**Purpose:** Living implementation roadmap and continuity record for the complete cTrader indicator redesign.

---

## 0. Why this document exists

This document is the single continuity source for the ongoing cTrader indicator redesign.

The implementation must not depend on remembering details from a previous chat. Every completed phase, architectural decision, resolved defect, remaining defect, validation result, and next step must be recorded here.

**Workflow rule:**

1. Each phase is handled in a dedicated conversation turn/message.
2. A phase is not considered complete because code was edited; it is complete only after its acceptance gates are satisfied.
3. At the end of every implementation phase, this document must be updated with:
   - phase status
   - exact files/versions changed
   - important architectural decisions
   - defects found
   - defects fixed
   - tests/validation run
   - known limitations
   - next phase
4. Previous numbered indicator versions remain preserved. Do not overwrite a previous frozen version.
5. Structural problems must be fixed at the correct architectural layer, not by adding local exceptions or duplicated conditions.
6. The implementation line beginning after v73 is a **clean architectural line**. v73 is the behavioral reference and recovery baseline, not the architecture to be mechanically patched forever.

---

# 1. Continuity: work already completed before this roadmap

The following work was already performed in the v69-v73 line and must be retained as design knowledge. It must not be accidentally reintroduced as regressions.

## 1.1 v69 baseline and audit

Reference:
- `docs/ctrader/CFIP-V69-PHASE-01-AUDIT.md`
- Baseline source: `integrations/ctrader/calude-edit-v69.cs`

The v69 audit identified the major structural risks:

- live-plan lifecycle could be detached from an actually live broker position
- structural invalidation could clear strategy state before broker exit confirmation
- pending fills could escape managed lifecycle ownership when protection was missing
- partial TP state could be consumed before broker close success
- configuration and runtime execution state were coupled
- normal/aggressive/pending execution duplicated policy logic
- target logic was distributed across multiple consumers
- mutable Decision/Reaction/ExecutionModel/Plan state was shared across domains
- telemetry state was session-memory only
- daily-loss baseline was not guaranteed to represent the true day start
- existing pending orders were not necessarily part of the loss-circuit-breaker lifecycle
- EOD behavior was historically inconsistent with current configuration
- at least one declared parameter was not wired
- manual action UI conflicted with the intended automatic trading product model

The v69 baseline remains frozen and recoverable.

## 1.2 v70 hardening

Reference:
- `docs/ctrader/CFIP-V70-PHASE-02-ARCHITECTURE.md`
- Source: `integrations/ctrader/calude-edit-v70.cs`

v70 hardened the immediate lifecycle and mutation problems:

- live telemetry timeout no longer terminates broker ownership
- structural invalidation requests broker exit and waits for lifecycle confirmation
- managed pending fills always enter managed lifecycle ownership
- missing SL/TP becomes explicit recovery state
- partial TP mutation is result-aware
- broker SL/TP mutation is centralized through result-aware gateways
- close/cancel paths are result-aware
- broker lifecycle subscriptions were expanded to positions opened/modified/closed and pending order created/modified/filled/cancelled
- event handlers are observational/reconciliation paths rather than direct broker side-effect authorities

v70 was deliberately **not** the final modular architecture.

## 1.3 v71 MTF/time contract

Reference:
- `docs/ctrader/CFIP-V71-PHASE-03-MTF-TIME.md`
- Source: `integrations/ctrader/calude-edit-v71.cs`

v71 established important time integrity:

- one M5 decision reference
- closed-bar MTF evaluation
- closed chart confluence mapping
- cTrader server UTC (`TimeInUtc`) for runtime timing
- user-facing local time derived from platform user offset
- closed-bar trigger confirmation separated from live-price execution

The remaining architectural work is to make these contracts type-level and immutable instead of relying on shared mutable fields.

## 1.4 v72 Structure / Zones / Liquidity

Reference:
- `docs/ctrader/CFIP-V72-PHASE-05-STRUCTURE-ZONES.md`
- Source: `integrations/ctrader/calude-edit-v72.cs`

v72 hardened:

- BUY/SELL structural symmetry
- FVG mitigation
- full FVG consumption safety
- Order Block mitigation
- live execution zone discovery versus historical retest checks
- liquidity sweep penetration depth
- FVG/OB confluence behavior

The remaining work is to convert these helpers into one authoritative structural model rather than repeatedly rediscovering zones.

## 1.5 v73 execution hardening

The v73 line contains additional work completed immediately before this roadmap, including commits covering:

- unified closed trigger definition
- separation of closed trigger from live market execution
- accepted-fill ownership/reconciliation
- explicit execution intent
- aggressive fill integrity
- pending execution intent contract
- explicit decision policy modes
- static tests for the execution intent contract

Relevant recent commits include:

- `04cf10897611fef54e9b4226cf0c428c3cfb3d7f` — start v73 unified entry trigger execution review
- `e4851c1f4ee2472da0f1a8de9511769cb9458b05` — separate v73 closed trigger from live market execution
- `bc628c939f9e5fd58cdd73d152cbd11779695f6c` — harden v73 accepted fill ownership and reconciliation
- `3dd1296c4cc04d45cca7d556991aa09a82cc2ed1` — unify v73 closed trigger definition
- `0b6c04ca6f2ac049ae1f4205f6be8002c5a61a87` — add v73 execution intent and aggressive fill integrity
- `bda179033139c6ddb7484c55fc8a0e1c06f13aa1` — complete v73 pending execution intent contract
- `e2cbf123bcfab31a4936bb8d2893fffeea322078` — finish v73 explicit decision policy wiring
- `a86e18ca9ab126a3428019428413bff8a1950ec3` — add v73 execution intent static tests

Current reference source:
`integrations/ctrader/calude-edit-v73.cs`

Current v73 static test coverage includes checks for:

- explicit `ExecutionIntent`
- unified policy modes: Confirmed / Soft / Aggressive / Pending
- validation before market broker execution
- pending stop/limit intent validation
- aggressive actual-fill rebasing

These improvements are retained as requirements for the clean architecture, but they will be re-expressed through a stronger type and service boundary rather than duplicated in a monolithic class.

---

# 2. Product requirements that are now fixed

These are product-level requirements and must not be lost during refactoring.

## 2.1 Trading model

The indicator must support:

- intelligent market analysis
- MTF analysis
- structural analysis
- FVG
- Order Block
- liquidity
- smart entry
- structural SL
- smart target ladder
- automatic market trading
- automatic pending orders
- live position management
- broker protection
- reversal management
- outcome telemetry
- confidence calibration
- replay/scenario validation

## 2.2 No manual trade-entry/order-placement controls

The indicator must **not** expose manual BUY/SELL/STOP/LIMIT entry buttons.

The trading decision and order placement must be automatic when the relevant runtime switches and safety gates permit execution.

Pure safety controls such as closing managed positions or cancelling managed pending orders may exist, but they must be classified as safety controls, never disguised as manual entry/order controls.

## 2.3 Entry / Trigger terminology must be unambiguous

The clean architecture must use these definitions:

- **IdealEntry:** preferred execution price inside the structural execution zone.
- **EntryZone:** allowed structural price region for a retest-style execution.
- **Trigger:** structural activation threshold that turns a waiting setup into an executable breakout/continuation condition.
- **RequestedEntry:** exact price/quote sent to the broker for the selected execution mode.
- **ActualFill:** the broker-confirmed fill price.
- **Invalidation:** structural/risk boundary at which the setup is no longer valid.

These terms must never be used interchangeably.

## 2.4 Smart SL/TP

Broker default SL/TP behavior must not be treated as the strategy.

The engine creates a structural protection/target plan and then synchronizes that plan with broker-held protection through explicit broker mutation and reconciliation.

## 2.5 One strategy, one coherent decision

The M5/M15/HTF analysis, structural model, Entry/Trigger, SL, TP ladder, auto-trading, pending orders, chart lines, arrows, panel text and alerts must consume the same authoritative state.

No UI-only decision and no execution-only alternative decision may exist.

---

# 3. Clean architecture target

The final system is not a collection of independent helper methods. It is a deterministic pipeline with explicit contracts.

Target architecture:

```
                 ┌──────────────────────────┐
                 │      cTrader Runtime     │
                 └────────────┬─────────────┘
                              │
                     Market / Broker Input
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  01. Runtime Snapshot    │
                 │  time / quote / account  │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  02. MTF Snapshot        │
                 │  closed indices / bars   │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  03. Market Model        │
                 │  structure / regime      │
                 │  zones / liquidity       │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  04. Decision Engine     │
                 │  direction / confidence  │
                 │  evidence / gates        │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  05. Trade Plan Engine   │
                 │  Entry / SL / TP ladder  │
                 │  RR / validity           │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  06. Execution Planner   │
                 │  market / stop / limit   │
                 │  sizing / safety         │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  07. Broker Gateway      │
                 │  execute / modify / exit │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  08. Broker State        │
                 │  actual position/order   │
                 └────────────┬─────────────┘
                              │
                              ▼
                 ┌──────────────────────────┐
                 │  09. Lifecycle Manager   │
                 │  state transitions       │
                 └────────────┬─────────────┘
                              │
                ┌─────────────┴─────────────┐
                ▼                           ▼
      ┌───────────────────┐       ┌───────────────────┐
      │ 10. Live Manager  │       │ 11. Outcome/Calib │
      │ SL/TP/reversal    │       │ telemetry/feedback│
      └─────────┬─────────┘       └─────────┬─────────┘
                └─────────────┬─────────────┘
                              ▼
                 ┌──────────────────────────┐
                 │ 12. Presentation State    │
                 │ panel / chart / alerts   │
                 └──────────────────────────┘
```

---

# 4. Architectural invariants

These rules are mandatory.

## INV-01 — Direction

```
BUY  = +1
SELL = -1
WAIT =  0
```

No other direction convention is allowed.

## INV-02 — Closed-bar decision reference

One M5 closed-bar reference defines the analytical snapshot for a decision cycle.

No higher timeframe may silently use a forming bar.

## INV-03 — Pure analysis

Analysis functions do not:

- place orders
- cancel orders
- close positions
- modify broker SL/TP
- draw chart objects
- mutate lifecycle state

## INV-04 — Decision is not execution

A valid Decision does not imply a valid broker execution.

Decision -> Plan -> Execution Eligibility -> Intent -> Broker.

## INV-05 — Plan is not broker state

The strategy plan and broker state remain separate.

The engine may want one thing; the broker may currently hold another. The discrepancy must be represented explicitly.

## INV-06 — Broker is authoritative after side effects

After broker mutation, the confirmed broker object becomes the source of truth for the actual position/order state.

## INV-07 — Lifecycle follows broker reality

Telemetry timeout, chart redraw, UI cleanup, failed planning or alert emission cannot declare a broker position closed.

## INV-08 — One execution policy

Normal market, aggressive market, pending stop and pending limit execution share the same safety/eligibility policy pipeline.

Only the entry mechanism differs.

## INV-09 — One target ladder

There is one authoritative target ladder:

```
TP1 < TP2 < TP3 < TP4``` for BUY by price, and the inverse ordering for SELL.

The active broker target is a separate pointer into that ladder.

## INV-10 — No backward protection movement

SL can only move in the protective direction.

The broker target cannot move backward when the policy forbids it.

## INV-11 — Presentation is downstream

The chart and panel render state; they never create authoritative trading state.

## INV-12 — No silent fallback

Fallbacks must be explicit, typed and visible in state/provenance.

Example:

```
STRUCTURAL_STOP
HTF_STOP
ATR_FALLBACK
SYNTHETIC_TARGET
```

A fallback must never silently look like a structural result.

## INV-13 — Every parameter has a disposition

Every public parameter must be:

- ACTIVE
- DEPRECATED (with migration path)
- REMOVED

No dead/ambiguous controls.

## INV-14 — BUY/SELL symmetry

Every directional algorithm must be reviewed as paired BUY/SELL logic.

## INV-15 — Idempotent execution

A repeated Calculate/tick/event must not create duplicate orders, duplicate positions, duplicate lifecycle transitions or duplicate broker mutations.

---

# 5. Canonical domain models

The final architecture should converge on immutable or effectively immutable snapshots.

## 5.1 RuntimeSnapshot

Contains:

- server UTC time
- chart/local display time if needed
- symbol
- bid
- ask
- spread
- symbol trading status
- market hours status
- account equity
- free margin
- broker constraints
- current managed position/order counts

No analysis result belongs here.

## 5.2 MtfSnapshot

Contains:

- M5 reference time
- closed index for M1/M5/M15/M30/H1/H4/D1/W1
- closed-bar validity
- per-timeframe frame snapshot
- data completeness status

## 5.3 MarketFrame

Contains:

- OHLC
- ATR
- RSI
- ADX/DMI
- EMA
- momentum
- trend
- structure
- MSS
- CHOCH
- displacement
- liquidity
- FVG
- OB
- equal highs/lows
- optional confluence evidence
- regime-related measurements

## 5.4 StructureSnapshot

A normalized structural event ledger, not a bag of booleans.

Examples:

- BOS_BULL
- BOS_BEAR
- MSS_BULL
- MSS_BEAR
- CHOCH_BULL
- CHOCH_BEAR
- DISPLACEMENT_BULL
- DISPLACEMENT_BEAR
- LIQUIDITY_SWEEP_BULL
- LIQUIDITY_SWEEP_BEAR

Every event should carry timestamp/index, price, timeframe, quality and provenance.

## 5.5 ZoneSnapshot

A canonical zone representation:

- id
- type (FVG / OB / supply / demand / etc.)
- direction
- timeframe
- original bounds
- current managed bounds
- creation bar/time
- age
- mitigation state
- retest state
- invalidation state
- quality
- provenance

## 5.6 LiquiditySnapshot

Explicit liquidity objects:

- equal highs/lows
- prior day/week liquidity
- session liquidity
- swing liquidity
- forecast candidates
- sweep state
- distance
- quality
- timeframe
- provenance

## 5.7 DecisionSnapshot

Contains:

- direction
- confidence
- quality
- edge
- MTF agreement
- independent evidence
- structural confirmations
- regime
- regime quality
- **decision eligibility**
- policy mode
- exact block reasons
- provenance

DecisionSnapshot does **not** decide Entry/Trigger eligibility. Phase 7 owns Entry/Trigger state and must consume this snapshot rather than reconstructing direction/policy independently.

## 5.8 TradePlan

Contains:

- direction
- execution mode
- ideal entry
- execution zone
- trigger
- invalidation
- requested entry policy
- structural stop
- target ladder
- RR metrics
- level quality
- plan validity
- created-at/bar
- plan identity

## 5.9 ExecutionIntent

Contains:

- intent id
- strategy id
- symbol
- direction
- execution kind
- requested entry
- trigger
- SL
- effective target
- volume
- risk model
- expiry if pending
- policy mode
- idempotency key
- plan id
- timestamp

## 5.10 ExecutionResult

Contains:

- accepted/rejected
- order id
- position id
- actual fill
- requested vs actual delta
- broker error
- protection state
- reconciliation status

## 5.11 BrokerStateSnapshot

Contains actual:

- managed positions
- pending orders
- broker SL
- broker TP
- actual volume
- actual entry
- realized/unrealized P/L
- order status
- broker-side identity

## 5.12 LifecycleState

A formal state machine with explicit transitions.

## 5.13 OutcomeEvent

Contains:

- plan/position identity
- entry
- exit
- result
- reason
- R multiple
- max favorable excursion
- max adverse excursion
- target stages reached
- protection changes
- decision metadata at entry
- calibration bucket

---

# 6. Phase plan

The phases below are implementation phases. Each later phase depends on the contracts established in earlier phases.

---

## PHASE 0 — Continuity and freeze baseline

**Status: COMPLETE**

### Objective

Freeze v73 as the behavioral/reference baseline and absorb all prior work into the new roadmap.

### Completed

- reviewed prior v69/v70/v71/v72 architecture documents
- reviewed current v73 execution changes
- identified the product requirements that must remain invariant
- established that the post-v73 implementation line is a clean architecture line
- established this document as the continuity source

### Exit gate

Complete when the repository has a recoverable baseline and a documented roadmap.

---

## PHASE 1 — Architecture foundation and canonical contracts

**Status: COMPLETE — 2026-09-27**

### Objective

Build the new internal architecture before changing behavior.

### Work

1. Define canonical domain models.
2. Define immutable configuration snapshot.
3. Define runtime state.
4. Define lifecycle state machine.
5. Define broker identity.
6. Define execution identity/idempotency.
7. Define direction conventions.
8. Define error/result contracts.
9. Define time/MTF snapshot contract.
10. Define ownership boundaries.

### Mandatory rules

- no domain service directly accesses UI
- no UI code calls trading APIs
- analysis layer is side-effect free
- broker gateway is the only mutation gateway
- lifecycle manager is the only owner of lifecycle transitions

### Deliverables

- canonical nested classes or separated source files
- architecture comments only where they explain invariant/ownership
- explicit contracts
- compile-safe skeleton
- dependency diagram updated in this document

### Acceptance

- domain ownership is unambiguous
- all current high-coupling fields have a planned owner
- no circular dependency between analysis/execution/UI

---

## Phase 1 completion record

**Implementation version:** `integrations/ctrader/calude-edit-v74.cs`  
**Phase document:** `docs/ctrader/CFIP-V74-PHASE-01-ARCHITECTURE.md`

### Files added

- `integrations/ctrader/calude-edit-v74.cs`
- `docs/ctrader/CFIP-V74-PHASE-01-ARCHITECTURE.md`
- `tests/unit/test_ctrader_v74_architecture.py`

### Commits

- `dda8d524417cd448ccb99abf29ed540e59e6407d` — v74 foundation
- `5a73d5817ada0e2df29e6ba09e4c7f21aaebfdd5` — explicit cTrader API namespace / compile-safety correction
- `a130dceb1f33307389d22e4ccdcb55441c9c9c2b` — Phase 1 architecture document
- `4828c005fa5026eadc73856ebbd8f26d31fee465` — architecture contract tests

### Completed architectural work

- canonical BUY/SELL/WAIT direction contract
- separate IdealEntry / EntryZone / Trigger / RequestedEntry / ActualFill / Invalidation semantics
- separate Decision / TradePlan / ExecutionIntent / ExecutionResult / BrokerState contracts
- explicit lifecycle authority and transition graph
- explicit trade identity and mandatory execution idempotency key
- single directional TargetLadder contract with TP ordering validation
- separate execution envelope limits for chase, broker slippage, breakout-fill deviation and plan rebasing
- configuration snapshot separated from runtime snapshot
- explicit provenance/fallback model
- broker mutation boundary introduced as `ICFIPClean74BrokerGateway`
- presentation boundary introduced without granting presentation any trading authority
- v74 host reduced to a compile-safe orchestration shell; no manual entry UI and no direct broker mutations

### Defects/risks found during Phase 1

- The first v74 foundation draft omitted the explicit `cAlgo.API` namespace import. This was corrected before marking the phase complete.
- Full real cTrader runtime compilation is intentionally not claimed as completed in this phase.

### Validation

Source-level contract checks were added for:

- canonical direction
- entry terminology
- plan/intent/result/broker separation
- lifecycle authority
- idempotency
- target ordering
- execution-envelope separation
- broker gateway boundary
- absence of chart/trading side effects
- service boundaries
- v74 host shape
- balanced source braces
- absence of manual entry controls

### Phase 1 acceptance decision

**COMPLETE at the architecture-contract level.**

The foundation is ready for behavior migration. No v73 file was overwritten.

### Next phase

**Phase 2 — Configuration and parameter architecture**

## PHASE 2 — Configuration and parameter architecture

**Status: COMPLETE — 2026-09-27**

### Objective

Eliminate parameter duplication, conflicting thresholds and hidden configuration precedence.

### Work

1. Inventory all public parameters.
2. Map each parameter to one consumer.
3. Identify duplicate semantics.
4. Merge equivalent controls.
5. mark deprecated aliases where presets require compatibility.
6. create grouped configuration objects:
   - Decision
   - MTF
   - Structure
   - Zones
   - Liquidity
   - Indicators
   - Entry
   - Risk
   - Targets
   - Live Management
   - Filters
   - Execution
   - Alerts
   - Display
7. define explicit precedence.
8. create one immutable configuration snapshot at initialization/change boundary.

### Specific cleanup targets

Examples from v73:

- MinimumSmartQuality vs SmartQualityThreshold vs MinimumAutoSmartQuality
- MinimumEntryQuality vs MinimumEntryLocationQuality
- structural SL alias/repricing/trailing controls
- multiple target fallback and RR parameters
- duplicate cooldown concepts
- duplicate smart-quality/evidence thresholds

### Acceptance

Every parameter is ACTIVE/DEPRECATED/REMOVED and all effective values can be traced to one source.

---

## Phase 2 completion record

**Implementation version:** `integrations/ctrader/calude-edit-v75.cs`  
**Phase document:** `docs/ctrader/CFIP-V75-PHASE-02-CONFIGURATION.md`  
**Test:** `tests/unit/test_ctrader_v75_configuration.py`

### Completed

- preserved all **513/513** v73 public parameters in v75 in the same order, with no duplicate public parameter property names;
- established `CFIPClean75ConfigSnapshot` as the single effective configuration carrier for the complete parameter surface;
- grouped configuration into explicit canonical domains: Decision, MTF, Structure, Zones, Liquidity, Indicators, Entry, Risk/Targets, Live Management, Filters, Alerts, Automation, Smart Execution, Display, Intelligence, Confluence and Safety/Precision;
- separated configured Auto Trading / Automatic Orders values from `CFIPClean75RuntimeAuthority`;
- split parameter semantics conceptually rather than merging similarly named thresholds across different layers;
- identified the confirmed duplicate protection control `AutoProtectBrokerPositions` → `AutoBrokerProtection` for deprecation/migration;
- removed manual trade-entry authority from the clean architecture contract; `ShowTradeActionButtons` is compatibility-only and cannot be an execution authority;
- retained safety-control concepts separately from manual entry controls;
- established a complete parameter inventory with an explicit ACTIVE/DEPRECATED/REMOVED disposition for every parameter.

### Critical signal/trading integrity decision

No configuration consolidation was allowed to weaken signal quality or execution safety.

The following remain explicitly separated for later migration:
- global Decision thresholds vs Auto-Execution thresholds;
- general Smart thresholds vs Smart Engine overlays;
- Entry quality vs Entry-location confluence;
- Entry/Trigger distance vs broker slippage vs fill deviation vs plan rebase;
- structural stop limits vs fallback stop policy;
- strategy TP ladder vs effective broker TP stage;
- market auto-trading vs pending-order automation;
- broker protection vs runtime execution switches.

This preserves the higher-level intelligence and allows later phases to make these relationships explicit instead of collapsing them into one threshold.

### Validation

Repository-side source validation confirmed:

- v73 parameter count = 513;
- v75 parameter count = 513;
- parameter names and order are identical;
- no duplicate v75 parameter names;
- braces are balanced;
- one configuration snapshot construction occurs during initialization;
- runtime authority is separate from configuration;
- no direct cTrader broker mutation calls exist in the v75 shell;
- manual-entry authority is explicitly unsupported;
- manifest contains 513 parameter records.

### Commits

- `555e6bffe3fc41054c3587a8b2fcf82598b9fab2` — v75 configuration architecture
- `9f6b1dd4a5e6b6495334ca2309700d34957596a5` — complete parameter inventory
- `391fa0944d2488cd183a602212528dcaa221f0ab` — configuration integrity tests
- `c43f1e422ce468649c65d6f0290d1cbb965d1c6f` — parameter declaration correction
- `f319d5532cb38af629476526ac08f6b416517dac` — broker-boundary test correction
- `99a81fb296c253f418a5fe7f755dff665df51bd4` — final parameter-attribute correction

### Phase 2 acceptance decision

**COMPLETE at the configuration/architecture-contract level.**

The clean line now has a complete, traceable configuration surface. No functional parameter was silently discarded.

### Next phase

**Phase 3 — Time, MTF and data pipeline**

## PHASE 3 — Time, MTF and data pipeline

**Status: COMPLETE — 2026-09-27**

### Objective

Turn the successful v71 time contract into a first-class immutable data pipeline.

### Work

- canonical M5 reference
- closed-index resolver
- missing-data handling
- MTF snapshot creation
- server UTC runtime clock
- user display time
- no forming-bar leakage
- explicit chart-timeframe mapping
- bar availability diagnostics

### Audit targets

Every helper must be checked for hidden:

- Count - 1
- forming-bar use
- host chart index substitution
- inconsistent reference timestamps

### Tests

- M1/M5 boundary
- M15 boundary
- M30 boundary
- H1/H4/D1/W1 boundaries
- weekend/missing-bar
- timezone/session boundary

### Acceptance

A decision snapshot is temporally coherent across every timeframe.

---

## Phase 3 completion record

**Implementation version:** `integrations/ctrader/calude-edit-v76.cs`  
**Phase document:** `docs/ctrader/CFIP-V76-PHASE-03-MTF-TIME.md`  
**Test:** `tests/unit/test_ctrader_v76_mtf_time.py`

### Completed

- converted the v71 closed-bar rules into a first-class `CFIPClean76MtfSnapshot`;
- established one canonical M5 analytical reference derived from the M5 series;
- separated analytical reference time from current server UTC runtime time;
- added user-local display time using the cTrader platform offset;
- created per-timeframe closed-bar snapshots for M1/M5/M15/M30/H1/H4/D1/W1 plus the host chart;
- created one shared `ResolveClosedBar()` resolver based on cTrader `Bars.OpenTimes.GetIndexByTime()`;
- guaranteed that the final potentially-forming series item is never returned as a closed analysis bar;
- required the selected bar's next open to be at or before the M5 reference;
- added explicit reference freshness and typed MTF data status;
- separated primary decision readiness from optional D1/W1 history availability;
- added explicit temporal coherence validation for downstream Signal/Decision consumers;
- initialized all eight MTF series in the v76 host;
- kept Signal, Entry, Auto Trade, Auto Orders and broker mutations out of Phase 3 so the time/data contract remains side-effect free.

### Important corrections during Phase 3

- restored the explicit pre-history guard from v71 for references earlier than the first available bar;
- corrected coherence semantics so missing optional D1/W1 history is not mistaken for temporal leakage;
- corrected the new MTF test to scope the builder method precisely when a different configuration method is also named `Build()`.

### Trading integrity carried forward

The next Signal and execution phases must consume the v76 MTF snapshot rather than independently resolving indices. This is now a mandatory integration contract for:
- Signal direction and confidence;
- M5/M15/HTF structure;
- Entry and Trigger;
- automatic market execution;
- automatic pending orders;
- SL/TP planning;
- live-management event timing.

No forming-bar value may become evidence simply because a downstream helper has access to raw `Bars`.

### Validation

Direct repository/source validation confirmed:
- v75/v76 parameter parity remains 513/513 in identical order;
- v76 source braces are balanced;
- required M1/M5/M15/M30/H1/H4/D1/W1 contracts exist;
- a single MTF builder is used from the host calculation cycle;
- closed-bar resolver guards the final series item and requires `nextOpen <= reference`;
- UTC/local time sources are separated;
- no `DateTime.Now` or `DateTime.UtcNow` usage was introduced;
- no direct broker mutation or chart authority was introduced;
- version isolation from v75 is preserved.

A direct pytest runner attempt was blocked by the execution sandbox's inability to resolve `raw.githubusercontent.com`; therefore test execution is not claimed as passed. Final real cTrader compile/runtime validation remains a Phase 16 gate.

### Commits

- `4219e33099139bfc3f3260cb8c49f15597daf7d0` — v76 MTF/time pipeline
- `7e181df5f8fd073a04a483b6f9684a034784e243` — Phase 3 document
- `f5d1801f6d63f899e53e7d369cc01d4a11347076` — Phase 3 tests
- `6c8f4512e2ac9ef310cded8ceb5f599c21115965` — test scope correction
- `9fba770d63e8f74bb212fe312a7f2374b309e04c` — pre-history/coherence hardening
- `8cbaeb4da1b5d7c2187d79f8c3ba9d576910e861` — boundary tests
- `0f8237da49d955d4230102f0767ae2ab3ffba7d3` — final Phase 3 validation notes

### Phase 3 acceptance decision

**COMPLETE at the architecture/data-contract level.**

The time/MTF layer now provides a single coherent input boundary for the later Signal and trading layers.

### Next phase

**Phase 4 — Market model: indicators, regime and confluence**

## PHASE 4 — Market model: indicators, regime and confluence

**Status: COMPLETE — 2026-09-27**

### Objective

Create one normalized market model instead of adding independent scores everywhere.

### Work

- native indicator snapshots
- EMA trend/slope
- RSI
- ADX/DMI
- ATR
- MACD
- VWAP
- volume expansion
- healthy volatility
- regime detection
- choppiness/compression/transition
- directional evidence normalization

### Key rule

The same underlying evidence must not be counted multiple times simply because different downstream functions consume it.

### Acceptance

Every feature has a clear:
- raw measurement
- interpretation
- direction
- quality
- whether it contributes to evidence
- whether it is a gate
- whether it is presentation-only

---

## Phase 4 completion record

**Implementation version:** integrations/ctrader/calude-edit-v77.cs
**Phase document:** docs/ctrader/CFIP-V77-PHASE-04-MARKET-MODEL.md
**Test:** tests/unit/test_ctrader_v77_market_model.py

### Completed

- created one normalized CFIPClean77MarketModel;
- added a cTrader-native indicator catalog for each MTF Bars series using EMA, ATR, RSI and Directional Movement System;
- preserved v73 market-feature semantics for EMA trend/slope, RSI, ADX/DMI, ATR, two-bar momentum, rejection, volume expansion, EMA-difference MACD bias, rolling VWAP and healthy volatility;
- converted regime from string semantics to typed CFIPClean77Regime;
- separated BiasDirection from MarketQuality so market quality cannot manufacture a directional signal when bias is WAIT;
- created one explicit feature-evidence record for Trend, Momentum, RSI, DMI, EMA Slope, Rejection, Volume Expansion, MACD, VWAP and Healthy Volatility;
- preserved the v73 RSI exhaustion penalty;
- made the regime threshold consume configured AdxMinimum rather than silently hard-coding 20;
- normalized directional scores while keeping final trade direction ownership in the future Decision Engine;
- kept Market Model strictly upstream of broker execution, lifecycle and presentation.

### Important review correction

An intermediate source-edit operation matched the prefix of CFIPClean77MarketModelBuilder while removing the old model, which temporarily removed the builder and structural classes. This was detected by type-count and source review before acceptance.

The structural domain segment from v76 was then restored exactly after the version/namespace transformation, duplicate native indicator declarations were removed, and the final source was revalidated.

### Trading integrity

Phase 4 does not create a competing signal engine. The Market Model produces market evidence only. The upcoming Decision Engine remains the sole owner of final directional qualification, policy gates and signal state.

The following capabilities remain preserved for later phases: complete MTF analysis; BOS/MSS/CHOCH and displacement; FVG/OB; liquidity pools/sweeps; smart Entry/Trigger; structural SL; unified TP ladder; automatic market trading; automatic pending orders; broker protection; live management and partials; reversal/exhaustion; outcome/calibration.

### Validation

Direct repository/source validation confirmed:
- v76 parameter surface = 513;
- v77 parameter surface = 513;
- names and parameter order remain identical;
- no duplicate public type declarations;
- source braces are balanced;
- exactly one Market Builder, Native indicator catalog/set and Structure/Zone/Liquidity domain set;
- ten unique market features are registered;
- closed MTF snapshot indices drive frame construction;
- the market frame rejects the final potentially-forming bar;
- the v76 structural domain segment was restored without content drift after version transformation;
- no direct broker mutation or chart authority exists in v77;
- no v76 legacy references remain in the v77 implementation.

The repository contains a dedicated static test suite for these contracts. A direct pytest runner is not claimed here; final real cTrader compile/runtime validation remains the Phase 16 acceptance gate.

### Current Phase 4 commits

- 3b2242ca698edfcfa0cd0c59b418f796c0f556ec — initial v77 market model
- cb69cecb7f5aec98cd5e4e860f1af5868e3ab1e1 — separate market quality from directional bias strength
- 1c2095d4047236b532b523b9a8919d2bc9b8e70f — restore market builder and structural domain
- aeaf0d50fdd38aeb013353db693a813872dc0650 — remove duplicate native catalog
- 68547e2b84f43a6950637fcf63bc65d7c0413898 — preserve RSI exhaustion/configurable regime threshold
- a66129dcb6e4aa2e3ae50182aa715a11d6819312 — market model test corrections
- 1736adb04a0203930ff879a7c0f2c72bf318d986 — final test syntax/scope correction

### Phase 4 acceptance decision

COMPLETE at the architecture/market-model contract level.

The v77 Market Model is now the single upstream analytical source for the next Signal/Decision phases.

### Next phase

Phase 5 — Structure, FVG, OB and Liquidity ledger
## PHASE 5 — Structure, FVG, OB and Liquidity ledger

**Status: COMPLETE — 2026-09-27**

### Objective

Replace repeated ad-hoc discovery with one canonical structural ledger.

### Work

- swing model
- BOS
- MSS
- CHOCH
- displacement
- FVG creation
- FVG mitigation
- FVG invalidation
- OB creation
- OB quality
- OB mitigation
- liquidity pools
- equal highs/lows
- liquidity sweep
- prior day/week/session levels
- provenance
- zone identity
- zone lifecycle

### Important redesign

Instead of:

```
FindNearestFvg()
FindNearestOrderBlock()
FindEqualHigh()
...
```

being independently called everywhere, the market snapshot should carry a validated structural map that entry/stop/target consumers query.

### Acceptance

Entry, stop and target engines consume the same structural objects.

---

## Phase 5 completion record

**Implementation version:** integrations/ctrader/calude-edit-v78.cs
**Phase document:** docs/ctrader/CFIP-V78-PHASE-05-STRUCTURE-ZONES.md
**Test:** tests/unit/test_ctrader_v78_structure_ledger.py

### Completed

- created one canonical StructureSnapshot and StructureLedgerBuilder;
- normalized BOS, MSS, CHOCH, displacement, confirmed swings and liquidity-sweep events;
- created explicit FVG and Order Block records with identity, provenance, age and lifecycle;
- implemented FVG three-candle detection plus optional two-bar imbalance detection;
- preserved FVG partial mitigation control and the non-resurrection safety invariant after full consumption;
- preserved OB displacement requirement, body/full-range selection, mitigation, consumption and expiry;
- created explicit liquidity records for equal highs/lows, swings, prior day/week, configured session, daily pivot/R1/R2/S1/S2 and forecast candidates;
- separated liquidity Side from SweepDirection so target/entry consumers can query overhead/underfoot liquidity without semantic overload;
- centralized zone/FVG/liquidity confluence;
- created PremiumDiscountState as a context object rather than a final signal;
- wired StructureSnapshot into EngineState so future Signal, Entry, Stop and Target engines can consume the same structural authority;
- removed the legacy duplicate StructureEvent/ZoneSnapshot/LiquiditySnapshot type family from v78.

### Trading/signal integrity

Phase 5 does not own the final BUY/SELL signal and does not execute trades. It is the single structural evidence source upstream of Phase 6 Decision and Phase 7 Entry/Trigger.

Automatic market trading, automatic pending orders, smart SL/TP, broker protection, live management, partials, reversal/exhaustion and outcomes remain preserved as downstream capabilities.

The intended dependency is now:

`MTF Snapshot -> Market Model -> Structure Snapshot -> Decision -> Trade Plan -> Execution`

No downstream phase should rediscover FVG/OB/liquidity independently.

### Validation

Repository/source validation confirms:
- v77/v78 parameter surface = 513/513 with identical names and order;
- braces are balanced;
- no duplicate public type declarations;
- exactly one canonical StructureLedgerBuilder, StructureSnapshot, StructureEventRecord, ZoneRecord and LiquidityRecord;
- legacy duplicate structural type declarations are absent;
- BOS/MSS/CHOCH/displacement/sweep coverage exists;
- FVG includes three-candle and optional two-bar imbalance semantics;
- partial mitigation and non-resurrection are represented;
- OB mitigation/displacement/age controls remain represented;
- equal liquidity, sweep depth, prior day/week, session, pivots and forecast state are represented;
- confluence is centralized;
- StructureSnapshot wiring is present in EngineState and the calculation cycle;
- structure analysis is driven by the canonical closed-MTF snapshot;
- no direct broker mutation or chart authority exists in v78;
- no v77 legacy references remain.

A direct pytest runtime execution is not claimed in the sandbox. Real cTrader compilation/runtime remains the final Phase 16 release gate.

### Phase 5 commits

- e5faa462ef656a22f2b7b8d353fbefd67f905fa8 — initial v78 structural ledger
- 35c6721e9e1b0a9efde515bece0dbc5ab227ae21 — forecast liquidity
- 673dade67ed4f51b96741545555dd01c98607de8 — remove legacy duplicate structural types
- 517d981e1e225484e4bdd750754be5fa31bfff6d — two-bar FVG / partial mitigation
- 1ace48324fa32e276277da4acbc73fd24dfa41e5 — structural coverage tests
- 3e622e1855a14f28493fca3d4c61235c0f74c92b — documentation update
- e77ea9adb5acd58389b5d8af5a2b23d421d20896 — canonical structural test
- cdbdfbb2f494526d08a4541766c647f882764c4f — legacy type documentation
- 517d981e1e225484e4bdd750754be5fa31bfff6d — final FVG semantic correction
- d84eaee6d6485df0bcea5ba9b4c7255c9dbf82c4 — final legacy-type test correction

### Phase 5 acceptance decision

COMPLETE at the architecture/structural-contract level.

The structural ledger is ready to become the single upstream source for Phase 6 Signal/Decision.

### Next phase

Phase 6 — Decision engine
## PHASE 6 — Decision engine

**Status: IN PROGRESS — 2026-09-27**

### Objective

Build one authoritative decision pipeline.

### Work

- directional aggregation
- evidence model
- confidence
- edge
- smart quality
- timeframe agreement
- structural confirmation
- retest quality
- regime adaptation
- consensus
- hard gates
- soft policy
- aggressive policy
- pending policy
- exact block reasons
- canonical decision eligibility distinct from Phase 7 Entry/Trigger eligibility
- snapshot-level policy consistency invariants

### Critical redesign

Separate:

```
Evidence
→ Score
→ Quality
→ Eligibility
→ Policy
```

These are different concepts.

### Prevent double counting

The engine must detect when multiple indicators are measuring essentially the same underlying phenomenon and prevent artificial confidence inflation.

### Acceptance

Every execution path receives the same DecisionSnapshot.

### Phase 6 implementation record — 2026-09-27

**Current implementation:** v79 (integrations/ctrader/calude-edit-v79.cs)

**Commits:**
- eb9aae213a34995374e9b8489de5ed147ae2569f — fix Phase 6 decision evidence deduplication and missing Decision state setter
- c5ccefebb50b5abdc1180d0fa827aa1887f0c7bb — extend Phase 6 source-level validation
- 307ac7adb48d6e36fbd337e5d72b73089920dccf — preserve reference-gated market/structure state across Calculate cycles
- 32617129ed375067b269f4048c352371c39bf7ae — validate cycle-state retention and decision reuse
- 6912347a8e9a08c41fefabc26c6db9b37e0b7bde — preserve feature weights while deduplicating market evidence
- 8321ce0f53d90e3762522428299deacc0b87a04a — validate weighted market-evidence deduplication
- ae1d25b983f10ec2546a29efb6fc05bd188ba37d — harden DecisionSnapshot invariants and directional structural confirmations
- b7f3e4cc95a354553aead1e96a3237633a094fd3 — define FeatureAggregate and make market-feature deduplication order-independent
- e7f5fbe3083b6ccc0b129653374f38ba098571ff — add order-independent market evidence validation
- 16ec11e2f44a086d9134817b634c1362293e85fb — reconcile adaptive smart decision semantics
- 9d0892d2223a63001d36970b58db6888df7a8d57 — keep zero and tied evidence neutral
- fa30d58c95f0bf1798555f1a534276bdf7a353fe — reconcile Premium/Discount location bias and HTF confidence penalty
- 6f3cb428cac9bc04a26d6bccb01c6f1249b30c9a — validate adaptive Phase 6 semantics
- 19bc33ce80b3328cc31b7b20362a5cbb031ca4b2 — validate legacy decision semantics reconciliation
- a2232aa1f50d4fd32ffe983cf1a85e007ed5cb6b — harden DecisionSnapshot invariants and advanced-confluence policy
- 01cc3d28de9abf452f38ff1adfbbea5d7cd6e0c7 — validate decision-state invariants and confluence switch
- 485d1aea2070186bfc00daaef5e311037ced01fb — clarify Phase 6 parameter ownership boundaries
- 1dab6fb870f2a666926b81d230278c56ed9c01b1 — clarify Decision-quality ownership of legacy Entry filter
- 7f11fc037e5faa5685d642a2705c417349d1af0b — validate final Decision ownership boundaries
- c6e371ce02fa5bf041cc52ba2d7bd8d503104299 — deduplicate structural zone and liquidity evidence
- 8d1004063c8d1b26fa15ad811f71df9b1fd97790 — validate zone/liquidity evidence deduplication
- dac22c231f47cd322ed58289bdd61437ee43a7d9 — isolate v79 structure/liquidity identities
- 7ec17839b34ca03211c02eff417bf8cb913a21dc — validate v79 identity isolation
- 6a5ff5b7576b4226efffc1bc268a23a95245ac23 — separate zone quality from confluence modifier
- 781ee452795b562e55eb949cf29f9b997b22be9c — correct v79 parameter surface documentation
- cbccfd240b9e62555a4d0dd8c35618ec2ec5190c — validate zone/confluence separation
- 9f40efaf86c56b47ea476b8c73b9f653c1e17450 — enforce deferred entry/execution filter ownership
- 453dc0ca63e44b730d25571514b21e3312d71f56 — keep neutral decision block reasons exact
- 986a48698b25ffc1a0c748e47ab4aa569f1991f4 — validate neutral block-reason isolation
- c4a7807892a1e427bd75fce0e12b801dd65edfc6 — align Phase 6 source validation with canonical decision semantics

**Implemented:**
- authoritative CFIPClean79DecisionEngine
- canonical CFIPClean79DecisionSnapshot
- explicit Evidence → Score → Quality → Eligibility → Policy separation
- decision-level eligibility is explicitly separated from Phase 7 entry/trigger eligibility
- DecisionSnapshot rejects contradictory eligible/blocked and policy states
- structural, zone, liquidity, retest, regime and MTF inputs
- exact block-reason collection with de-duplication
- Confirmed / Aggressive / Pending / Soft policy modes
- Decision state setter so the authoritative snapshot is actually persisted in cycle state
- market evidence de-duplication by CFIPClean79MarketFeature across timeframes; MTF agreement remains a separate domain
- structure-event family de-duplication for BOS/MSS/CHOCH and Displacement
- structural confirmation count is direction-specific and deduplicated by structural-break family per M5/M15/H1/H4 timeframe, with M5 displacement as a separate confirmation
- opposing-direction structural evidence cannot satisfy the selected direction's structural gate
- adaptive smart thresholds are restored from v73 semantics using regime-aware quality/share/edge policy without reusing Trigger/Entry gates
- directional shares use the configured SmartScoreTemperature softmax and exact zero/tie evidence remains WAIT
- Premium/Discount is preserved as location bias only and does not increment independent evidence
- HigherTfPenalty is preserved as a confidence-risk penalty for H1/H4/D1 opposition, separate from directional evidence
- structural Zone evidence is family-deduplicated per direction (FVG/OB), preventing record-count inflation
- liquidity evidence is family-deduplicated per direction by liquidity-pool kind, preserving distinct pool categories without repeated-timeframe inflation
- v79 structure/liquidity record identities are version-isolated (`CFIP79|`); no v78 identity prefix remains
- ZoneQuality no longer includes FVG/liquidity confluence bonuses; confluence is represented through typed flags and consumed by Decision quality once
- legacy Entry Location, Structural Sequence and Proxy Expected Value filters remain deferred to their Phase 7/8 owners and cannot re-enter Phase 6 Decision logic
- when directional consensus is WAIT, only the root `NoDirection` gate is emitted; confidence/MTF/structure/quality directional gates remain inactive until a direction exists
- `UseAdvancedConfluence` now explicitly controls the confluence contribution to Decision quality
- DecisionSnapshot now enforces WAIT/block/eligibility/policy consistency at construction time
- legacy `UseSmartEntryQualityFilter` is explicitly treated as a Decision-quality floor in Phase 6, not as Entry/Trigger eligibility
- legacy Trigger/Entry parameters are explicitly retained for preset parity but not interpreted by the Phase 6 decision owner; Phase 7 owns those semantics
- legacy `AdaptiveRegimeWeighting` is explicitly superseded by deduplicated evidence plus regime-adaptive policy to avoid a second directional weighting layer
- Feature aggregation now keeps strongest BUY and SELL observations separately; exact ties contribute no directional evidence, eliminating timeframe-order bias
- added the missing internal `FeatureAggregate` type required by the deduplication implementation
- confluence retained as a quality modifier rather than a second directional evidence source
- no broker mutation or UI execution authority introduced in Phase 6

**Validation added:**
- parameter-surface preservation against v78
- decision-engine contract checks
- structure/zone/liquidity consumption checks
- no broker/UI authority checks
- decision-state setter check
- market evidence de-duplication checks
- structure-event family de-duplication checks
- weighted quality-domain checks
- brace/type/version isolation checks
- stale market-evidence assertions replaced with weighted-deduplication assertions
- decision policy consistency and single-evaluation authority checks
- adaptive threshold and softmax checks
- zero/tie direction safety checks
- legacy location-bias and HTF-penalty checks
- snapshot-state invariant checks
- advanced-confluence switch checks
- explicit parameter-ownership/disposition checks for deferred Trigger/Entry controls
- final Decision Engine boundary check: exactly one final direction assignment and no broker/UI mutation
- zone-family and liquidity-pool-family evidence deduplication checks
- neutral-direction block-reason isolation checks
- version-isolated structure/liquidity identity checks
- independent ZoneQuality/Confluence checks
- corrected parameter-surface baseline checks (513)
- deferred Entry/Execution filter ownership checks

**Latest findings/fixes:** a compile-critical missing internal type was found: v79 referenced `FeatureAggregate` without defining it. This is now fixed. A second decision-integrity defect was also found: equal-strength opposing observations of one market feature could inherit the answer from timeframe loop order. The aggregator now resolves BUY/SELL strengths symmetrically and treats exact ties as neutral.

A structural-gate semantic defect was also found during deep audit: the prior deduplicated event counter could not reliably reach the default `MinimumStructuralConfirmations=4` for a single selected direction because it collapsed opposing and multi-timeframe confirmations into one global counter. v79 now preserves the v73 confirmation intent while deduplicating BOS/MSS/CHOCH within each timeframe and selecting only the chosen direction. `DecisionSnapshot` now also enforces policy/eligibility consistency at construction time.

**Validation status:** source-level tests are committed, including order-independent aggregation, adaptive threshold, softmax, zero/tie safety, location-bias and HTF-penalty checks. An execution attempt from the current environment could not download the GitHub test files because external DNS/network access is unavailable here; therefore no passing pytest result is claimed. The Phase-7 v80 consumer now receives the exact Phase-6 DecisionSnapshot; real cTrader compilation/runtime remains pending.

**Remaining before Phase 6 completion:**
1. downstream-consumer audit is now structurally enforced: v80 Entry receives _state.Decision directly and does not recompute direction
2. same-snapshot propagation is enforced by exact host wiring: one Decision evaluation feeds one Entry evaluation
3. source-level v73-v78 semantic reconciliation for the audited Phase-6 areas is complete; no additional source-level Phase-6 gap is currently known
4. run the complete applicable automated test suite; the new GitHub static-test workflow is configured, but its push-run result was not exposed by the available connector in this session
5. real cTrader compilation/runtime validation remains mandatory; Phase 6 is therefore not marked COMPLETE

---

## PHASE 7 — Entry / Trigger / Retest / Breakout engine

**Status: IN PROGRESS — PRE-ACCEPTANCE IMPLEMENTATION, 2026-09-27**

### Objective

Completely eliminate the Entry/Trigger ambiguity that has repeatedly appeared in earlier versions.

### Canonical model

```
Structural Zone
     ↓
Ideal Entry
     ↓
Retest Eligibility
     │
     └───────── OR ────────┐
                           ↓
                    Breakout Trigger
                           ↓
                     Trigger Reached
                           ↓
                    Requested Entry
                           ↓
                      Actual Fill
```

### Work

- retest entry
- breakout entry
- continuation entry
- reversal entry
- execution envelope
- trigger tolerance
- broker fill tolerance
- late-entry/chase tolerance
- spread-aware entry
- stale setup handling
- plan expiry
- entry invalidation

### Acceptance

The chart line, panel, plan and execution intent all agree on what Entry and Trigger mean.

---

## Phase 7 implementation record — 2026-09-27

**Current implementation:** v80 (integrations/ctrader/calude-edit-v80.cs)

**Phase document:** docs/ctrader/CFIP-V80-PHASE-07-ENTRY-TRIGGER.md

**Commits:**

- 3af7c78e5e6f6d185dde28ba7841a9329673ce8e — start v80 Phase 7 entry/trigger implementation
- 27215d35e36d667224ec4bebb9994cbf949943da — harden stale-setup and precision-entry gates
- 1681fb5bdb48d5398c55915de6f6c958739338c4 — add Phase 7 architecture tests
- 14ee16b479e141187bf33993c1af8345d4bb910b — correct Phase 7 source assertions
- 088d0f99aea1661760e9b9a660f1ee118a245ad8 — record Phase 7 architecture and validation
- 7ab75ecdb6d4587b47756c348fce3ce525a029ab — harden retest detection and typed trigger evidence
- b589a3f9ea37c49a1ae4e098a326d586d9f1e6c4 — make blocked entry timestamps deterministic
- d597d62d4426db0046752d126a488e62b236c425 — remove invalid zone midpoint property usage
- 958de27e84c398333c6582cf95ad6a82eea3e285 — eliminate remaining invalid zone midpoint references
- 35f78e12b36375c1cf166af8320fa4691c4449f9 — remove inconsistent entry timestamp handling
- 0cada1be318bd1a2919a7f256637e38aab73ffd0 — enforce typed trigger evidence and retest touch semantics
- ffd3ec8683e3b39b22a241f979c568b98049634a — validate Phase-7 chronology guards
- 97e2aae5e87537d8bbffa91cdf06af7331abae34 — align order-block retest parameter key
- 99c1549e4b4af49324ca46b5ba55f68cb7e63f7d — validate canonical order-block retest key
- e4fac0af0add7d84a3999ac6edaa0cc84b2c4016 — add cTrader static-test workflow

**Implemented:**

- canonical EntrySnapshot carrying the exact Phase-6 DecisionSnapshot
- one Entry/Trigger engine consuming the authoritative decision
- retest-market path using structural FVG/OB zones
- breakout-trigger path using recent M5 BOS/MSS/CHOCH/Displacement
- explicit IdealEntry, EntryZone, Trigger, RequestedEntry separation
- Trigger/RequestedEntry remain distinct from future broker ActualFill
- M1/M5 confirmation gates
- spread-aware entry blocking
- retest quality/close/rejection gates
- precision-entry quality/distance controls
- stale setup / expiry protection
- setup invalidation
- continuation-stop and reversal-limit proposal modes without broker mutation
- no UI/chart or broker authority

**Validation added:**

- exact v79/v80 parameter parity at 513/513
- balanced braces, unique declarations and version isolation
- no invalid ZoneRecord midpoint usage
- exactly one Decision evaluation and one Entry evaluation
- direct _state.Decision propagation
- no Broker/UI mutation in the Entry engine
- retest/breakout/trigger/stale/expiry/invalidation/spread/precision source checks
- trigger evidence is isolated from generic market IndependentEvidence
- breakout trigger candle-body/range/close-location gates are wired
- chronological guards prevent retroactive retests and future trigger events
- all Phase-7 configuration keys used by Entry resolve to the canonical 513-parameter surface
- 24 dedicated Phase-7 source-level tests

**Acceptance status:**

Phase 7 is not accepted yet. Real cTrader compilation and controlled runtime/scenario validation remain mandatory gates.

---

## PHASE 8 — Risk, Stop and Reward model

**Status: IN PROGRESS — PRE-ACCEPTANCE IMPLEMENTATION, 2026-09-27**

### Objective

Create one risk/SL/target foundation before broker execution is changed.

### Work

- structural stop
- M5 structure
- HTF structure
- zone-based stop
- spread-aware minimum risk
- maximum structural risk
- risk preference
- risk/quality balance
- fallback policy
- target candidate generation
- target ranking
- TP1-TP4
- RR
- HTF reward
- liquidity reward
- obstacle detection
- target provenance

### Key rule

Target generation is one engine.

No consumer independently rebuilds a different TP ladder.

### Phase 8 implementation record — 2026-09-27

**Current implementation:** v81 (`integrations/ctrader/calude-edit-v81.cs`)

**Phase document:** `docs/ctrader/CFIP-V81-PHASE-08-RISK-TARGETS.md`

**Static tests:** `tests/unit/test_ctrader_v81_phase8_risk_targets.py`

**Commits:**
- 3559572ae6b63967d7f994be0f7ee1f00ffe4b08 — start v81 Phase 8 risk/stop/target architecture
- bb128fbaa606e116b12c3ffab59f7fb65b3fcf48 — record Phase 8 deep-audit corrections
- ea6062b97f30576880636608f96a6ef685f456d8 — add Phase 8 source-level validation
- 554ec09139436a4b62abdcf36187f02556beb15 — record Phase 8 architecture
- c5299be7e852c43bac9ab73c7a6844903990044b — correct Phase 8 provenance assertion

**Implemented:**
- one authoritative `CFIPClean81TradePlanBuilder`
- exact Phase-6 DecisionSnapshot + Phase-7 EntrySnapshot consumption
- snapshot identity coherence check; no direction recomputation
- structural stop hierarchy: Entry invalidation → M5 FVG/OB → M5 swing → optional H1/H4/D1 swing
- explicit minimum/maximum structural-risk ATR bounds
- explicit ATR fallback with typed provenance when structural policy permits fallback
- one authoritative TP1–TP4 target ladder builder
- configured RR floors, adaptive quality-based RR expansion, target spacing, clearance and maximum extension
- liquidity target selection with HTF provenance
- opposing-zone obstacle detection
- explicit synthetic RR target fallback
- target ladder directional ordering validation
- downstream Plan persistence after Entry evaluation
- no broker mutation, order placement, live management or UI authority in Phase 8

**Important integrity rule:**
Phase 8 never reconstructs direction or entry semantics. The TradePlan is downstream of the exact Decision/Entry snapshots. A fallback stop/target is explicitly marked through `CFIPClean81Provenance` and cannot silently masquerade as structural.

**Acceptance status:**
Phase 8 is not accepted yet. Phase 7 runtime acceptance remains a prerequisite for the complete release chain, and Phase 8 additionally requires real cTrader compilation plus controlled risk/target scenario validation.

**Deep-audit corrections:**
- target candidates must progress monotonically beyond the previous TP in the selected direction by the configured spacing;
- `SignalId` and `PlanId` are now distinct identities;
- valid TradePlan construction enforces direction/entry/protection/TP1 invariants.

---

## PHASE 9 — Unified execution policy and broker gateway

**Status: IN PROGRESS — PRE-ACCEPTANCE IMPLEMENTATION, 2026-09-27**

### Objective

Make automatic trading actually reliable and eliminate policy drift between market/aggressive/pending paths.

### Work

Create one pipeline:

```
Execution Eligibility
→ Risk/Sizing
→ Broker Constraints
→ Intent
→ Idempotency Check
→ Broker Gateway
→ Result
→ Reconciliation
```

### Modes

- Confirmed
- Soft
- Aggressive
- Pending

### Execution kinds

- Market
- Stop
- Limit

### Shared safety checks

- trading permission
- market hours
- margin
- daily loss
- position limit
- spread
- volatility
- suitability
- plan validity
- execution envelope
- broker min distances
- volume constraints

### Phase 9 implementation record — 2026-09-27

**Current implementation:** v82 (`integrations/ctrader/calude-edit-v82.cs`)

**Phase document:** `docs/ctrader/CFIP-V82-PHASE-09-EXECUTION.md`

**Static tests:** `tests/unit/test_ctrader_v82_phase9_execution.py`

**Workflow:** `.github/workflows/ctrader-static.yml`

**Commits:**
- 6014b117ee77d3db9d7f4e6973ca493d06db4791 — initial v82 unified execution implementation
- 2b9efc84062455c9826e371dfe83f388b6173bbe — authoritative TradePlan execution anchor
- 0d07d588f6bdab60360b42ab8308c999a22e85bf — execution safety/lifecycle hardening
- bda9f2cd11e5df85424b8b125c7f8348838d065f — execution-anchor risk amount correction
- 226717218d8c55386efced2a10cdfb8aa0d7b3d3 — broker distance/volume/history hardening
- fc609c075b021c508200288ea6cf4cabd659f7c0 — session and Friday guards
- 7da517949c30c5d956b924e01248bf51422535a2 — pending limit identity comment correction
- b735c644349c9b757678278026e74c47c8317fbc — remove silent TP-stage fallback
- bbeba5a3de82c7973077efc7fb4ff8581589f65f — include Phase 8/9 tests in CI
- d02e31ed11b9d2822b3834c335210e43f6c41277 — initial Phase 9 tests
- db6defda320aecf476c5c520721041efaec308bb — target-stage test correction
- eca0533b9469617c2c4a140e283714f3947d816a — initial Phase 9 documentation
- c37662e51cfd6a4766910ee0eeeadb9db6ef514c — Phase 9 documentation audit update

**Implemented:**
- one ExecutionPolicy owner for Market / Stop / Limit paths
- exact DecisionSnapshot + EntrySnapshot + TradePlan coherence checks
- explicit market auto-trading vs automatic pending-order enablement
- risk sizing from TradePlan.ExecutionAnchor + structural stop
- proportional-risk and fixed-lot volume handling with broker normalization
- explicit risk-budget guard
- cTrader estimated-margin guard using current account margin/free margin plus the estimated new order margin
- broker minimum SL/TP distance checks
- broker minimum/step volume checks
- session-window and Friday cutoff guards
- one ExecutionIntent planner
- one cTrader broker-mutation gateway
- pending-order expiry and persisted SignalId/PlanId broker comments
- broker-confirmed ActualFill mapping for market execution
- protection-state detection and recovery signaling
- local + broker + history-backed idempotency checks
- broker state reader and reconciliation
- lifecycle advancement on accepted/rejected broker execution
- explicit no-silent-fallback behavior for requested TP stage

**Deep-audit corrections:**
1. Plan now carries an explicit `ExecutionAnchor`; sizing and risk no longer default to `IdealEntry`.
2. Pending proposals can become valid Plans without pretending a waiting trigger is a market fill.
3. New-order margin is evaluated with cTrader's estimated-margin API instead of using current margin alone.
4. Target stage selection no longer silently falls back to TP1.
5. Pending Stop/Limit orders persist SignalId/PlanId in broker comments for durable duplicate detection.

**Validation status:**
Source-level structural checks currently confirm 513 parameters, balanced braces, version isolation, execution-interface alignment and the Phase-9 policy/gateway boundaries. The GitHub workflow was updated to include Phase 6, 7, 8 and 9 source tests, but an executed workflow result has not been exposed by the available connector. Real cTrader compilation and controlled broker scenarios remain mandatory.

**Current limitations before Phase 9 acceptance:**
- suitability, news/event and smart-volatility execution gates still need to be consolidated into the unified policy with authoritative input contracts; existing decision/entry layers already contain portions of these semantics, but Phase 9 has not yet claimed complete consolidation;
- execution-envelope/fill-deviation policy and plan-rebase semantics still require the dedicated runtime contract before they can be marked complete;
- pending-order lifecycle cleanup/fill ownership belongs to Phase 10 and is intentionally not claimed complete here;
- real cTrader runtime validation is still pending.

---

## PHASE 10 — Pending orders

**Status: IN PROGRESS — PRE-ACCEPTANCE IMPLEMENTATION, 2026-09-27**

### Objective

Make automatic order placement/removal a first-class lifecycle rather than an execution side branch.

### Work

- continuation stop
- reversal limit
- adaptive mode
- expiry
- cleanup
- stale order invalidation
- daily-loss cancellation
- symbol/order identity
- duplicate prevention
- fill event reconciliation

### Phase 10 implementation record — 2026-09-27

**Current implementation:** v83 (`integrations/ctrader/calude-edit-v83.cs`)

**Phase document:** `docs/ctrader/CFIP-V83-PHASE-10-PENDING.md`

**Static tests:** `tests/unit/test_ctrader_v83_phase10_pending.py`

**Workflow:** `.github/workflows/ctrader-static.yml`

**Commits:**
- ac50bf50f1b1684fa3d2ebe7eddff58581b86a3d — v83 first-class pending lifecycle
- d83e404feb0e43e5cf6d48221295825bb1328656 — protection recovery / identity hardening
- 2d8af2fb89e4fc28d02602e5f3e6e3f5839f384f — cancellation reconciliation hardening
- ce5f643a1508bc3500aa91515451a46916c1df00 — Phase 10 static tests
- dc8b9e4314f792a2477db66ac02e4b066ff9355b — Phase 10 documentation
- ca93c91b8e44f524c7bbc2c905c1cf041f157420 — CI coverage for Phase 10 tests
- 6cc4c2ac9daf4e666d82d29fa6936473c4750f60 — restart-safe pending broker reconciliation
- ba12020d5b07805ef65f4d6b51e75fbb6af7eb34 — restart reconciliation static tests
- 96b6741f11aac1c91c689248f57f34e681eef33d — restart reconciliation documentation
- 460c0115f9fc0e86a4033ca21ea649e4b00af20e — canonical execution-configuration key coverage test
- f45a8f2c5bcf0230e41ce31f4e6fbd07fd52fb09 — v83 broker identity comment format
- 6108881a97387fe98da77c1725149480537b79b5 — plan supersession / identity isolation tests
- e27061da293e3dbaf45d128d5d1736f6eaa9ef7d — stale pending cancellation on plan supersession
- 55a1b99bc017784e039409fb2400bf8406bf4769 — atomic pending protection gateway mutation
- f181ee28d31eb30906ab2f5ef9f723032e9bcee8 — atomic protection static test

**Implemented:**
- first-class pending-order lifecycle record/state
- managed-label + CFIP identity scoping
- startup adoption of existing managed pending orders
- SignalId / PlanId parsing from persisted broker comment
- Created / Modified / Filled / Cancelled event observation
- Filled event ownership transition to LivePosition
- pending protection recovery action
- post-fill protection recovery action
- expiry cancellation action
- daily-loss cancellation action
- broker mutation only through the unified gateway
- pending protection mutation through price-based PendingOrder methods
- deterministic cancellation reconciliation against actual broker state
- restart-safe pending->filled reconstruction through Position comment / PlanId
- explicit filled Position identity in the pending lifecycle record
- registration of accepted pending orders with expected plan protection/expiry
- explicit distinction between lifecycle observation and broker mutation
- canonical configuration-key coverage confirms all 101 execution/lifecycle configuration reads are present on the 513-parameter surface
- v83 PlanId/SignalId persistence is now version-consistent end-to-end
- known-plan pending orders are cancelled on authoritative plan supersession
- pending SL/TP recovery uses one absolute-protection gateway mutation

**Deep-audit rule:**
A pending-order event never becomes an independent trading authority. Events only update/reconcile lifecycle state or enqueue an action. Broker-changing actions flow through the single Phase-9 gateway.

**Validation status:**
Source-level structural checks cover the Phase-10 lifecycle, event wiring, recovery paths, startup adoption, identity parsing, gateway boundary and manual-entry safety. No passing pytest/workflow result is claimed. Current cTrader documentation confirms the event signatures and PendingOrder properties/mutation methods used by the design. citeturn835745view0turn557778view0

**Current limitations before Phase 10 acceptance:**
- stale-order invalidation based on setup supersession remains to be formalized;
- partial-fill / multiple-position handling requires controlled runtime scenarios;
- restart adoption and event ordering require real cTrader runtime validation;
- Phase 9 execution-envelope / fill-deviation / plan-rebase policies remain pending;
- real cTrader compilation/runtime remains mandatory.

---

## PHASE 11 — Position lifecycle and broker protection

**Status: IMPLEMENTED / RUNTIME VERIFICATION PENDING — v87 HARDENING, 2026-09-27**

### Objective

Make broker reality authoritative and recovery-safe.

### Work

- position adoption
- position identity
- fill rebasing
- broker SL/TP synchronization
- protection verification
- mutation result handling
- retry policy
- recovery state
- missing protection state
- position close confirmation
- orphan position detection
- restart recovery

### Critical invariant

Never clear the internal live-plan owner while the broker still has a managed live position.

### Phase 11 implementation record — 2026-09-27

**Current implementation:** v84 (`integrations/ctrader/calude-edit-v84.cs`)

**Phase document:** `docs/ctrader/CFIP-V84-PHASE-11-POSITION.md`

**Static tests:** `tests/unit/test_ctrader_v84_phase11_position.py`

**Implemented:**
- first-class managed Position lifecycle record/state
- startup adoption of managed live Positions
- Position Opened / Modified / Closed event observation
- actual broker entry rebasing after fill
- pending -> Position protection handoff
- protection drift detection
- protection recovery through the unified broker gateway
- restart-safe broker Position reconciliation
- explicit orphan Position state
- no automatic orphan close
- invariant preserving live broker ownership during reconciliation

**Current validation:** source-level checks are implemented; real cTrader compile/runtime remains mandatory.

**Remaining before acceptance:**
- close request/result/retry state machine
- partial-fill / multiple-position ownership
- protection retry/backoff policy
- broker disconnect/recovery scenarios
- controlled runtime validation
- governed orphan remediation policy

### Phase 11 deep re-audit / hardening record — 2026-09-27

**Current hardening:** v85 (`integrations/ctrader/calude-edit-v85.cs`)

**Re-audit document:** `docs/ctrader/CFIP-V85-PHASE-11-REAUDIT.md`

**Static tests:** `tests/unit/test_ctrader_v85_phase11_reaudit.py`

**Release-blocking corrections made:**
- aligned the host Position protection call with the actual `ICFIPClean85BrokerGateway.ModifyProtection` contract;
- corrected pending-fill handling so the actual broker Position id is recorded;
- moved expected Position protection mutation behind the Position record's own mutation method;
- removed inherited `CFIP79|` structure identities and standardized v85 identity markers;
- tightened broker Position/Pending adoption, exposure counts, duplicate detection and daily P/L reconstruction to the current v85 identity domain;
- strengthened target-ladder validation against stage ordering and execution-anchor direction;
- added pending-order expiry/live-side preflight gates;
- added final broker-gateway Stop/Target directional validation;
- retained the no-manual-trade-entry-control invariant.

**Validation boundary:** v85 static re-audit coverage is source-level. Actual cTrader compilation, broker-event scenarios and runtime recovery tests remain mandatory.

### Phase 11 v86 lifecycle-completion record — 2026-09-27

**Current implementation:** v86 (`integrations/ctrader/calude-edit-v86.cs`)

**Documentation:** `docs/ctrader/CFIP-V86-PHASE-11-LIFECYCLE.md`

**Tests:** `tests/unit/test_ctrader_v86_phase11_lifecycle.py`

**Additional v86 corrections:**
- Pending-order records now retain multiple resulting broker Position ids instead of overwriting the first fill identity.
- A filled PendingOrder is no longer used as the target of Position protection recovery; Position lifecycle owns the mutation.
- Expected protection is handed from Pending lifecycle to Position lifecycle after reconciliation, including restart/missed-event cases.
- Position close is an explicit request/result state machine; broker acceptance does not mark the position closed.
- Position protection failures and requested-close failures use deterministic bounded exponential backoff.
- Broker state reconciliation and mutation processors are skipped while `Server.IsConnected` is false, preventing stale snapshots from causing reconciliation/mutation.
- The clean v86 lifecycle path contains no `DateTime.Now` / `DateTime.UtcNow` calls.
- cTrader's current documentation confirms `Server.IsConnected` and `Server.TimeInUtc` are available on the Algo server interface, and cTrader documents full and partial position close operations through `ClosePosition`. citeturn213547search0turn967588search2

**CI correction discovered during re-audit:**
- The cTrader architecture workflow previously invoked pytest without installing it.
- The workflow now installs pytest explicitly and includes the v85/v86 test files.
- Two pre-existing Python syntax defects in v79/v80 tests were corrected.

**Acceptance boundary:** implementation is complete at the source/static-contract layer. Controlled real cTrader compilation, broker-event execution, rate-limit behavior, reconnect/recovery, partial-close execution, and runtime restart scenarios remain mandatory.

### Phase 11 v87 hardening / re-audit continuation — 2026-09-27

**Current hardening:** v87 (`integrations/ctrader/calude-edit-v87.cs`)

**Static tests:** `tests/unit/test_ctrader_v87_phase11_hardening.py`

**Implemented corrections carried forward from the v84-v86 audit:**
- repaired the missing multi-position broker-id backing state on pending orders;
- added explicit pending Cancel/Protection action state with result feedback and bounded retry/backoff;
- prevented immediate pending-order reconciliation from treating broker-event visibility lag as cancellation;
- made Position protection verification drift-aware for both Stop Loss and Take Profit;
- moved final Stop/Target direction and broker-distance validation into the broker gateway mutation boundary for initial execution and later mutations;
- changed exposure gating to account for managed Positions and Pending Orders together;
- required complete SignalId/PlanId execution identity before broker execution;
- allowed deterministic lifecycle adoption of broker-existing Pending Orders / live Positions during restart/recovery;
- added broker-confirmation grace to prevent a just-accepted order from being falsely interpreted as absent/closed;
- made event handlers runtime-null-safe;
- removed the obsolete `Show Trade Action Buttons` compatibility parameter; manual BUY/SELL/order-placement UI remains disabled while safety controls remain classified separately;
- removed system-clock dependencies from lifecycle logic; platform-provided runtime time remains the source of truth.

**Validation:** CI workflow run `36322249439` completed successfully with **206 passed** tests.

**Remaining acceptance boundary:** real cTrader compilation, controlled broker execution, partial-fill/reconnect/restart scenarios, rate-limit behavior and live broker event validation remain mandatory.

---### Acceptance

Simulated and real broker event sequences cannot create impossible internal states.

---

## PHASE 12 — Live management: SL, TP, partials, reversal, exhaustion

**Status: IMPLEMENTED / STATIC VERIFICATION COMPLETE / RUNTIME ACCEPTANCE PENDING — v88, 2026-09-27**

### Objective

Unify all in-trade management under a single LivePositionManager.

### Work

- break-even
- spread-aware BE
- risk-free lock
- structural SL repricing
- structural trailing
- dynamic target advancement
- partial TP
- target-stage state
- reversal protection
- fast reversal
- profit exhaustion
- structural invalidation
- EOD behavior
- protection conflict resolution

### Important redesign

Every live mutation is a state transition with a broker result.

Example:

```
TP1 REACHED
→ PARTIAL_CLOSE_REQUESTED
→ BROKER_ACCEPTED
→ PARTIAL_EXECUTED
→ TP1_CONSUMED
```

A failed mutation does not silently consume the state.

### Acceptance

All management actions are idempotent and retry-safe.

---

### Phase 12 v88 implementation / re-audit continuation — 2026-09-27

**Current implementation:** v88 (`integrations/ctrader/calude-edit-v88.cs`)

**Static tests:** `tests/unit/test_ctrader_v88_phase12_live_management.py`

**Implemented corrections:**
- established a Position-owned `LivePlanSnapshot` containing the original plan identity, direction, execution anchor, structural stop, invalidation and TP1-TP4 targets;
- made live Target/Partial management fall back to the Position-owned plan snapshot when the current market signal/plan has rolled over;
- preserved direction + PlanId matching for plan ownership;
- kept dynamic target advancement, break-even/risk-free protection, partial TP, reversal protection, profit-exhaustion protection, structural invalidation and end-of-day behavior inside the Live Position Manager;
- kept broker mutations behind the unified broker gateway and retained broker-result confirmation semantics;
- retained idempotent protection/partial/close state transitions and retry-aware handling;
- removed system-clock calls from the new Phase 12 path.

**Important remaining items before Phase 12 acceptance:**
- durable persistence of Position-owned live-plan snapshots across a full indicator restart when the broker comment cannot reconstruct the complete target ladder;
- controlled runtime verification of partial-close volume normalization, broker rejection/retry and reconnect scenarios;
- explicit scenario coverage for reversal, exhaustion, EOD and protection-conflict precedence;
- final real cTrader compile and broker execution validation.

**Current CI:** workflow run `36322249439` completed successfully with **206 passed** tests.


### Phase 12 v89 re-audit / hardening continuation — 2026-09-27

**Current implementation:** v89 (`integrations/ctrader/calude-edit-v89.cs`)

**Re-audit document:** `docs/ctrader/CFIP-V89-PHASE-12-LIVE-MANAGEMENT-REAUDIT.md`

**Static tests:** `tests/unit/test_ctrader_v89_phase12_reaudit.py`

**Implemented corrections:**
- synchronized replacement of queued Position protection mutations by explicitly clearing the stale pending-action flag;
- made the Position protection queue report whether a broker mutation was actually enqueued;
- restored missing SL/TP from the matching Position-owned/current Plan when the broker snapshot has lost protection;
- made accepted partial-close requests leave the permanently-latched pending state and enter bounded retry/recovery after the confirmation window;
- relaxed close retry pacing after accepted submission to reduce repeated mutations while broker confirmation propagates;
- corrected live Position protection validation so BE/risk-free/structural trailing SLs are validated against the current executable market side rather than the historical entry price;
- kept managed positions without a current SL eligible for lifecycle/live-manager registration after restart instead of silently dropping them from management;
- hardened broker-gateway mutation ownership so position/order mutation is rejected unless label, symbol and CFIP89 identity all match the managed strategy;
- initialized engine state and lifecycle authority before broker event subscriptions and existing-position adoption;
- preserved all v88 authority boundaries: live-manager intent, Position lifecycle ownership, and broker-gateway mutation.

**Static re-audit result:** PASS for brace balance, version isolation, main dependency declarations, v88 parameter-surface parity, multi-position fill storage, protection queue replacement, plan-owned protection restoration, partial-close retry, live protection validation, managed-object gateway ownership, initialization ordering, and absence of manual entry controls.

**Latest implementation commits:**
- `cef82c36e8415532be4f5b8b5614dd1f7d486ef6` — harden v89 live protection/restart adoption;
- `c35f60c4e181f565ba99576434ab102059d53ae1` — enforce managed broker ownership in v89 gateway.

**Runtime boundary:** real cTrader compilation and broker execution/recovery scenarios remain mandatory.

**Next step:** close Phase 12 runtime acceptance; only then begin Phase 13 outcome/telemetry/calibration implementation.

## PHASE 13 — Outcome, telemetry, calibration and feedback

**Status: NOT STARTED**

### Objective

Turn outcomes into a durable learning/diagnostics layer without corrupting live execution state.

### Work

- outcome events
- entry metadata
- exit metadata
- MFE/MAE
- R result
- stage progression
- false signal events
- calibration buckets
- directional performance
- persistence strategy
- restart recovery
- telemetry timeout as observation boundary only

### Calibration

Calibration must not directly mutate execution policy without an explicit policy layer.

### Acceptance

Telemetry cannot terminate lifecycle and restart does not silently erase required state.

---

## PHASE 14 — Presentation architecture

**Status: NOT STARTED**

### Objective

Make chart/panel/alerts a faithful rendering of authoritative engine state.

### Work

- presentation snapshot
- panel state machine
- chart state machine
- Entry line
- Ideal Entry line
- Trigger line
- Invalidation
- SL
- TP1-TP4
- active broker TP
- arrows
- prediction
- reaction
- watch state
- labels
- level prices
- alerts
- popup
- language separation
- theme/style separation

### Mandatory UI rule

No UI element may independently infer a different direction or trade plan.

### Safety controls

Retain only clearly classified safety controls when required.

No manual BUY/SELL/order-placement buttons.

### Acceptance

A single authoritative state produces consistent:

```
Panel ↔ Lines ↔ Arrows ↔ Labels ↔ Alerts ↔ Broker state
```

---

## PHASE 15 — Cleanup, modular extraction and performance

**Status: NOT STARTED**

### Objective

Remove the historical monolith and ensure maintainability.

### Work

- eliminate duplicated helpers
- eliminate dead code
- remove historical patch comments that no longer describe current logic
- eliminate ambiguous names
- group domain types
- extract services/classes where cTrader project structure permits
- reduce main Indicator responsibilities to orchestration
- eliminate hidden allocations in tick path
- cache stable objects
- avoid repeated full scans
- separate closed-bar and tick paths

### Target main indicator responsibility

Ideally the top-level Indicator becomes:

```
Initialize
→ Build Runtime
→ Read Runtime Snapshot
→ Run Closed-Bar Cycle
→ Run Tick Cycle
→ Reconcile Broker Events
→ Render Presentation
```

It should not contain the majority of market/business logic.

---

## PHASE 16 — Verification, compile/runtime hardening and release candidate

**Status: NOT STARTED**

### Objective

Prove the system.

### Static validation

- C# lexical balance
- duplicate methods
- duplicate parameters
- dead parameters
- forbidden direct broker mutations
- UI-to-broker dependency scan
- closed-bar leakage scan
- direction symmetry scan
- version isolation scan

### Unit tests

- time alignment
- direction
- score/quality
- structure
- FVG
- OB
- liquidity
- entry
- stop
- TP
- RR
- volume
- spread
- margin
- lifecycle
- pending
- partial close
- reversal
- EOD
- outcome

### Scenario/replay matrix

Required scenarios:

1. strong BUY continuation
2. strong SELL continuation
3. breakout
4. retest
5. reversal
6. false breakout
7. stale setup
8. wide spread
9. volatility shock
10. news blackout
11. insufficient margin
12. daily loss limit reached
13. pending order expiry
14. pending fill
15. partial TP success
16. partial TP rejection
17. SL mutation rejection
18. TP mutation rejection
19. position closure event
20. indicator restart with live position
21. orphan managed position
22. duplicate tick/event
23. EOD close
24. broker disconnect/recovery

### Real cTrader validation

The final gate must include a real cTrader compile and controlled execution test.

Repository-side static tests alone do not qualify as runtime proof.

---

# 7. New architecture: ownership map

| Responsibility | Sole intended owner |
|---|---|
| cTrader parameters | Configuration |
| Runtime toggles | Runtime State |
| Time/quotes/account | Runtime Snapshot |
| MTF indexing | MTF Snapshot Builder |
| indicators | Market Model |
| structure | Structure Engine |
| zones | Zone Ledger |
| liquidity | Liquidity Engine |
| regime | Regime Engine |
| final decision | Decision Engine |
| entry mode | Execution Planner |
| stop | Risk/Stop Engine |
| target ladder | Target Engine |
| sizing | Risk/Sizing Engine |
| eligibility | Execution Policy |
| broker mutation | Broker Gateway |
| broker reality | Broker State |
| lifecycle | Lifecycle Manager |
| live management | Live Position Manager |
| outcomes | Outcome/Telemetry |
| chart/panel/alerts | Presentation |
| safety close/cancel controls | Safety Controller |

---

# 8. Canonical execution state machine

```
FLAT
 │
 ├─ analysis only ───────────────► FLAT
 │
 ▼
SIGNAL_DETECTED
 │
 ▼
DECISION_READY
 │
 ├─ blocked ────────────────────► WAIT
 │
 ▼
PLAN_READY
 │
 ├─ expired / invalidated ──────► WAIT
 │
 ▼
EXECUTION_READY
 │
 ├─ WAIT_TRIGGER ───────────────► EXECUTION_READY
 │
 ├─ PENDING_STOP ───────────────► PENDING_ORDER
 │
 ├─ PENDING_LIMIT ──────────────► PENDING_ORDER
 │
 └─ MARKET ─────────────────────► BROKER_SUBMITTED
                                      │
                         ┌────────────┴─────────────┐
                         ▼                          ▼
                  BROKER_ACCEPTED              REJECTED
                         │                          │
                         ▼                          ▼
                  POSITION_LIVE                EXECUTION_ERROR
                         │
                         ▼
                  PROTECTION_SYNC
                         │
              ┌──────────┴──────────┐
              ▼                     ▼
        PROTECTED LIVE         RECOVERY_REQUIRED
              │
              ▼
        MANAGED_POSITION
              │
     ┌────────┼──────────┬──────────────┐
     ▼        ▼          ▼              ▼
   TP/SL   TRAIL      REVERSAL      INVALIDATION
     │        │          │              │
     └────────┴──────────┴──────────────┘
                    │
                    ▼
               EXIT_REQUESTED
                    │
                    ▼
              BROKER_CONFIRMED
                    │
                    ▼
                  CLOSED
                    │
                    ▼
                 OUTCOME
```

---

# 9. Canonical target state machine

The target ladder has two independent concepts.

## Strategy ladder

```
TP1
TP2
TP3
TP4
```

## Broker effective target

```
NONE
TP1
TP2
TP3
TP4
CUSTOM_ALLOWED
```

The broker target may advance as permitted, but strategy target history must remain auditable.

Partial-close state is separate:

```
TP1_LEVEL_REACHED
TP1_PARTIAL_REQUESTED
TP1_PARTIAL_ACCEPTED
TP1_CONSUMED
```

The same pattern applies to TP2.

---

# 10. Canonical Entry state model

```
WAIT
  ↓
ZONE_IDENTIFIED
  ↓
ENTRY_CANDIDATE
  ├──────────────┐
  ▼              ▼
RETEST           BREAKOUT
  │              │
  ▼              ▼
ZONE_VALID       TRIGGER_WAIT
  │              │
  ▼              ▼
MARKET_READY     TRIGGER_REACHED
  │              │
  └──────┬───────┘
         ▼
EXECUTION_INTENT
         ▼
BROKER
         ▼
ACTUAL_FILL
```

Important:

**Trigger is not the fill.**
**Ideal Entry is not the fill.**
**Requested Entry is not the fill.**

---

# 11. Error policy

Every error must belong to a category.

## Analysis errors

Result:

```
DATA_INVALID
DATA_INCOMPLETE
ANALYSIS_UNAVAILABLE
```

No broker action.

## Decision errors

Result:

```
DECISION_BLOCKED
DECISION_UNCERTAIN
```

No broker action.

## Execution errors

Result:

```
EXECUTION_REJECTED
EXECUTION_INVALID
BROKER_CONSTRAINT
```

No assumption of fill.

## Lifecycle errors

Result:

```
STATE_CONFLICT
ORPHAN_POSITION
ORPHAN_ORDER
RECOVERY_REQUIRED
```

Fail-safe reconciliation.

## Presentation errors

Result:

```
PRESENTATION_DEGRADED
```

Never affects trading authority.

---

# 12. Required provenance

Every meaningful trading output should be explainable.

Examples:

```
EntrySource
StopSource
Tp1Source
Tp2Source
Tp3Source
Tp4Source
DecisionEvidence
PolicyMode
BlockReason
ExecutionMode
BrokerMutationReason
```

A future debugging session must be able to answer:

> Why did CFIP decide this?
> Why was this entry selected?
> Why was this stop selected?
> Why was this TP selected?
> Why was the order blocked?
> Why was the broker target moved?
> Why was the position closed?

without reconstructing the answer from dozens of unrelated booleans.

---

# 13. v73 issues to carry into the clean architecture

These are not all necessarily still present as runtime bugs; they are architectural carry-forward items that must be explicitly resolved rather than forgotten.

## A01 — Monolithic main class

v73 still contains a very large cTrader Indicator class with hundreds of methods and parameters.

**Resolution:** domain-oriented extraction, first by ownership, then by source-file boundaries where supported.

## A02 — Mutable shared plan/decision/frame state

`_plan`, `_decision`, `_reaction`, `_executionModel` and frame fields are shared mutable state.

**Resolution:** immutable snapshots between pipeline stages.

## A03 — Target logic distributed

Target generation, selection, fallback, enrichment and broker synchronization exist in several consumers.

**Resolution:** one authoritative TargetSet.

## A04 — Execution policy duplication

Normal, aggressive and pending paths repeat safety and sizing policy.

**Resolution:** one ExecutionPolicy.

## A05 — Risk/entry tolerance ambiguity

The v73 code reuses several ATR-distance concepts in multiple meanings.

**Resolution:** explicit ExecutionEnvelope with separate limits.

## A06 — HTF concept inconsistency

Some helpers treat M15/M30 as HTF while others only count H1+ for selected HTF claims.

**Resolution:** one timeframe classification contract.

## A07 — Soft/hard block taxonomy drift

Block strings and policy checks can diverge.

**Resolution:** typed BlockReason enum plus display text mapping.

## A08 — Session/EOD contract drift

Historical documentation and current defaults have not always described the same behavior.

**Resolution:** one explicit Session/EOD policy.

## A09 — Telemetry persistence

Outcome/calibration state is memory-local in the current line.

**Resolution:** explicit persistence boundary or clearly defined session-only mode; final product should not silently lose important learning state.

## A10 — Daily-loss baseline

The baseline can depend on when the indicator was attached/restarted.

**Resolution:** explicit day-start equity policy using a durable/reconstructable source.

## A11 — UI contains safety controls and historical manual-action remnants

**Resolution:** distinguish safety controls from manual entry/order placement and remove unwanted manual trade entry.

## A12 — Static verification versus actual runtime validation

Repository tests are not equivalent to real cTrader compile/runtime execution.

**Resolution:** final phase requires actual cTrader validation.

---

# 14. Definition of done for the complete redesign

The redesign is complete only when all of the following are true:

1. The main Indicator is orchestration-focused.
2. Data, analysis, decision, planning, execution, broker state, lifecycle, live management and presentation have explicit owners.
3. Entry and Trigger have one shared definition.
4. Plan and broker state are explicitly separate.
5. Market/aggressive/pending execution share one policy layer.
6. SL/TP are produced by one risk/reward engine.
7. TP1-TP4 and effective broker TP are explicitly separated.
8. Broker mutations are result-aware.
9. Lifecycle cannot become closed while broker state is live.
10. Pending fills cannot become unmanaged.
11. No manual trade-entry/order-placement controls exist.
12. Safety controls are explicitly classified.
13. All public parameters have a disposition.
14. All BUY/SELL logic is symmetric.
15. Closed-bar analysis cannot leak forming-bar data.
16. Runtime tick paths are efficient.
17. Outcome telemetry does not control lifecycle ownership.
18. Restart/recovery is deterministic.
19. Static tests cover the major invariants.
20. Scenario/replay tests cover trading edge cases.
21. A real cTrader compile succeeds.
22. Controlled broker/test execution confirms actual orders, fills, SL/TP mutations and exits.
23. Chart/panel/alerts match the authoritative engine state.
24. The continuity document is updated with final version, commit, validation and known limitations.

---

# 15. Versioning rule for the implementation line

- v69 remains frozen.
- v70 remains the lifecycle-hardening historical line.
- v71 remains the MTF/time historical line.
- v72 remains the Structure/Zone historical line.
- v73 remains the latest behavioral reference/baseline.
- The next architectural implementation starts as a **new version/file**, preserving v73.
- Do not overwrite previous version files.
- Each implementation version must state its exact parent/reference version.

The exact next filename/version will be chosen when Phase 1 implementation starts, but it must remain independently recoverable.

---

# 16. Phase status ledger

| Phase | Name | Status | Completion date | Reference/version | Validation |
|---|---|---|---|---|---|
| 0 | Continuity & baseline freeze | COMPLETE | 2026-09-27 | v69-v73 + this roadmap | Repository/document review |
| 1 | Architecture foundation | COMPLETE | 2026-09-27 | v74 | static contract checks added |
| 2 | Configuration/parameters | COMPLETE | 2026-09-27 | v75 | 513/513 parity + static validation |
| 3 | Time/MTF/data | COMPLETE | 2026-09-27 | v76 | source-level MTF/time validation |
| 4 | Market model | COMPLETE | 2026-09-27 | v77 | source-level market model validation |
| 5 | Structure/Zones/Liquidity | COMPLETE | 2026-09-27 | v78 | canonical structural ledger + lifecycle coverage |
| 6 | Decision engine | SEMANTICALLY COMPLETE / VERIFICATION PENDING | 2026-09-27 | v79 | authoritative DecisionSnapshot; evidence de-duplication; v80 downstream snapshot contract enforced; runtime verification pending |
| 7 | Entry/Trigger | PRE-ACCEPTANCE IMPLEMENTATION | 2026-09-27 | v80 | canonical EntrySnapshot; retest/breakout trigger engine; runtime acceptance pending |
| 8 | Risk/SL/Targets | IMPLEMENTED / VERIFICATION PENDING | 2026-09-27 | v81 | TradePlan / structural stop / target ladder; runtime verification pending |
| 9 | Unified execution | IMPLEMENTED / VERIFICATION PENDING | 2026-09-27 | v82 | ExecutionPolicy / Intent / BrokerGateway; runtime verification pending |
| 10 | Pending orders | IMPLEMENTED / VERIFICATION PENDING | 2026-09-27 | v83 | pending lifecycle / reconciliation; runtime verification pending |
| 11 | Lifecycle/Broker | IMPLEMENTED / RUNTIME VERIFICATION PENDING | 2026-09-27 | v84-v87 | Position lifecycle, close/recovery ownership, multi-position pending handoff, retry/backoff, broker confirmation grace, disconnect safety and no-manual-entry surface; real runtime validation pending |
| 12 | Live management | IMPLEMENTED / STATIC RE-AUDIT PASS / RUNTIME ACCEPTANCE PENDING | 2026-09-27 | v88-v89 | Position-owned live-plan snapshot, BE/risk-free, structural SL repricing, dynamic TP, partial TP, reversal/exhaustion/invalidation/EOD management, protection-queue synchronization, live-position validation and managed-gateway ownership hardening; real cTrader runtime acceptance pending |
| 13 | Outcome/Calibration | NOT STARTED | — | — | — |
| 14 | Presentation | NOT STARTED | — | — | — |
| 15 | Cleanup/Performance | NOT STARTED | — | — | — |
| 16 | Verification/Release | NOT STARTED | — | — | — |

---

# 17. How every future phase must be executed

For every phase:

### Step A — Read before editing

Read:

1. this roadmap
2. the previous phase's completion notes
3. the current implementation version
4. relevant historical phase documents
5. relevant tests

### Step B — Audit

Identify:

- current behavior
- architectural violation
- defects
- dependencies
- regression risks

### Step C — Implement

Prefer:

- replacement of the underlying abstraction
- one source of truth
- deterministic data flow
- explicit state
- reusable contracts

Avoid:

- duplicate helper
- special-case exception
- branch added only to fix one screenshot
- hidden fallback
- string-based state where enum/type is appropriate
- UI-driven execution logic

### Step D — Validate

Run all applicable:

- static checks
- source checks
- unit tests
- scenario tests
- compile checks

### Step E — Update this document

At minimum record:

- status
- version/file
- commit SHA
- exact changes
- important findings
- resolved issues
- unresolved issues
- validation results
- next phase

---

# 18. Current position

**Current implementation reference:** v89

**Current authoritative continuation:** v87 is the Phase 11 hardening line; v88 is the Phase 12 live-management line; v89 is the Phase 12 re-audit/hardening line. v84-v86 remain historical Phase 11 implementation lines and are not the current continuation target.

**Current roadmap status:** Phases 6-12 have implementation lines present through v89; source/static/static re-audit gates are covered, while real cTrader compile/runtime acceptance remains pending for execution/lifecycle/live-management behavior.

**Current implementation status:** v79 contains the Phase 6 authoritative Decision engine on top of the v78 StructureSnapshot, v77 MarketModel and v76 MTF contracts; v80-v83 provide Entry/Risk/Execution/Pending implementation lines; v84-v87 complete the Lifecycle/Broker hardening line; v88 adds Position-owned Live Management; v89 hardens Phase 12 protection-queue replacement, plan-owned protection restoration, partial-close retry and close-confirmation pacing. v78 remains the Phase 5 line, v77 the Phase 4 line, v76 the Phase 3 line, v75 the Phase 2 configuration line, v74 the Phase 1 contract foundation, and v73 the behavioral/reference baseline.

**Current implementation target:** Phase 12 runtime acceptance for the v89 live-management line. Do not start Phase 13 implementation until the remaining Phase 12 acceptance gates are closed: real cTrader compilation, controlled broker execution, restart/reconnect behavior, partial-close confirmation/retry, and reversal/exhaustion/invalidation/EOD precedence scenarios.

**Critical instruction for the next phase:** Treat v89 as the current Phase 12 source of truth for runtime acceptance. Preserve the architectural authority chain `Decision -> Entry -> TradePlan -> ExecutionPolicy -> BrokerGateway -> Pending/Position Lifecycle -> LivePositionManager`, and do not bypass lifecycle/gateway ownership while resolving runtime failures.

**Continuity rule:** When this project is resumed in another chat, this document must be read first and the phase ledger above must be treated as authoritative.

---

# 19. Current progress chart

```
CFIP cTrader Clean Architecture

Phase 0  ████████████████████  COMPLETE
Phase 1  ████████████████████  COMPLETE
Phase 2  ████████████████████  COMPLETE
Phase 3  ████████████████████  COMPLETE
Phase 4  ████████████████████  COMPLETE
Phase 5  ████████████████████  COMPLETE
Phase 6  ████████████████████  IMPLEMENTED / RUNTIME VERIFICATION PENDING
Phase 7  ████████████████████  IMPLEMENTED / RUNTIME VERIFICATION PENDING
Phase 8  ████████████████████  IMPLEMENTED / RUNTIME VERIFICATION PENDING
Phase 9  ████████████████████  IMPLEMENTED / RUNTIME VERIFICATION PENDING
Phase 10 ████████████████████  IMPLEMENTED / RUNTIME VERIFICATION PENDING
Phase 11 ████████████████████  IMPLEMENTED / RUNTIME VERIFICATION PENDING (v87)
Phase 12 ████████████████████  IMPLEMENTED / STATIC RE-AUDIT PASS / RUNTIME ACCEPTANCE PENDING (v89)
Phase 13 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 14 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 15 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 16 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
```
