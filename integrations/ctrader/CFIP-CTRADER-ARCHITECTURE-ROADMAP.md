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
- entry eligibility
- policy mode
- exact block reasons
- provenance

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

- preserved all **512/512** v73 public parameters in v75 in the same order, with no duplicate public parameter property names;
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

- v73 parameter count = 512;
- v75 parameter count = 512;
- parameter names and order are identical;
- no duplicate v75 parameter names;
- braces are balanced;
- one configuration snapshot construction occurs during initialization;
- runtime authority is separate from configuration;
- no direct cTrader broker mutation calls exist in the v75 shell;
- manual-entry authority is explicitly unsupported;
- manifest contains 512 parameter records.

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
- v75/v76 parameter parity remains 512/512 in identical order;
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
- v76 parameter surface = 512;
- v77 parameter surface = 512;
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
- v77/v78 parameter surface = 512/512 with identical names and order;
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

**Implemented:**
- authoritative CFIPClean79DecisionEngine
- canonical CFIPClean79DecisionSnapshot
- explicit Evidence → Score → Quality → Eligibility → Policy separation
- structural, zone, liquidity, retest, regime and MTF inputs
- exact block-reason collection with de-duplication
- Confirmed / Aggressive / Pending / Soft policy modes
- Decision state setter so the authoritative snapshot is actually persisted in cycle state
- market evidence de-duplication by CFIPClean79MarketFeature across timeframes; MTF agreement remains a separate domain
- structure-event family de-duplication for BOS/MSS/CHOCH and Displacement
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

**Validation status:** source-level tests are committed, but they have not yet been executed in a local/CI environment from this turn. Phase 6 therefore remains **IN PROGRESS**.

**Remaining before Phase 6 completion:**
1. trace every existing signal/decision consumer and prove no parallel BUY/SELL authority remains
2. prove every downstream execution path consumes the same DecisionSnapshot instance/value
3. reconcile remaining v73-v78 decision semantics against the new pipeline
4. run the complete applicable test/compile suite
5. only then mark Phase 6 COMPLETE

---

## PHASE 7 — Entry / Trigger / Retest / Breakout engine

**Status: NOT STARTED**

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

## PHASE 8 — Risk, Stop and Reward model

**Status: NOT STARTED**

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

### Acceptance

Given one PlanSnapshot, every execution path obtains the same target ladder.

---

## PHASE 9 — Unified execution policy and broker gateway

**Status: NOT STARTED**

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

### Acceptance

No market/pending path duplicates the policy stack.

---

## PHASE 10 — Pending orders

**Status: NOT STARTED**

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

### Acceptance

Pending Order -> Filled always results in managed Position ownership.

Pending Order -> Cancelled always results in deterministic lifecycle reconciliation.

---

## PHASE 11 — Position lifecycle and broker protection

**Status: NOT STARTED**

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

### Acceptance

Simulated and real broker event sequences cannot create impossible internal states.

---

## PHASE 12 — Live management: SL, TP, partials, reversal, exhaustion

**Status: NOT STARTED**

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
| 2 | Configuration/parameters | COMPLETE | 2026-09-27 | v75 | 512/512 parity + static validation |
| 3 | Time/MTF/data | COMPLETE | 2026-09-27 | v76 | source-level MTF/time validation |
| 4 | Market model | COMPLETE | 2026-09-27 | v77 | source-level market model validation |
| 5 | Structure/Zones/Liquidity | COMPLETE | 2026-09-27 | v78 | canonical structural ledger + lifecycle coverage |
| 6 | Decision engine | IN PROGRESS | 2026-09-27 | v79 | authoritative DecisionSnapshot; evidence de-duplication added; downstream-consumer audit and validation remain |
| 7 | Entry/Trigger | NOT STARTED | — | — | — |
| 8 | Risk/SL/Targets | NOT STARTED | — | — | — |
| 9 | Unified execution | NOT STARTED | — | — | — |
| 10 | Pending orders | NOT STARTED | — | — | — |
| 11 | Lifecycle/Broker | NOT STARTED | — | — | — |
| 12 | Live management | NOT STARTED | — | — | — |
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

**Current implementation reference:** v79

**Current roadmap status:** Phase 6 in progress.

**Current implementation status:** v79 contains the Phase 6 authoritative Decision engine on top of the v78 StructureSnapshot, v77 MarketModel and v76 MTF contracts. v78 remains the Phase 5 line, v77 the Phase 4 line, v76 the Phase 3 line, v75 the Phase 2 configuration line, v74 the Phase 1 contract foundation, and v73 the behavioral/reference baseline.

**Current implementation target:** Phase 6 — Decision engine (v79); do not advance to Phase 7 until downstream-consumer tracing, DecisionSnapshot single-source validation, full applicable tests/compile checks, and remaining v73-v78 semantic reconciliation are complete.

**Critical instruction for the next phase:** Start from the v78 StructureSnapshot, v77 MarketModel and v76 MTF snapshot. Build one authoritative Decision engine that consumes the existing evidence exactly once, separates Evidence -> Score -> Quality -> Eligibility -> Policy, and becomes the sole source for Signal, Auto Trade and Auto Order eligibility.

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
Phase 6  ████████░░░░░░░░░░░░  IN PROGRESS
Phase 7  ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 8  ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 9  ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 10 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 11 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 12 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 13 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 14 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 15 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
Phase 16 ░░░░░░░░░░░░░░░░░░░░  NOT STARTED
```
