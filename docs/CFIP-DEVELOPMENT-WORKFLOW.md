# CFIP-PRO Development Workflow

**Status:** Living engineering workflow  
**Project:** CFIP-PRO  
**Reference capability source:** `CForex` only  
**Development:** Native Windows  
**Production:** Docker at the production-engineering stage  

---

## 1. Workflow Objective

This workflow defines the exact operating method for building CFIP-PRO in a controlled, repeatable, auditable manner.

Frontend and backend are developed in parallel. Open-source software is preferred wherever a mature component can provide the required capability. Custom code is minimized and reserved for product-specific domain logic, orchestration, contracts, policy, integration gaps, and other functionality that should not be delegated.

The workflow is a living document. When a discovery or architectural decision changes how the project should be built, this document and the master prompt must be updated before the new rule becomes part of the permanent process.

---

# 2. Permanent Development Loop

Every meaningful work unit follows this sequence:

```text
01 DISCOVER
     ↓
02 AUDIT CURRENT STATE
     ↓
03 DEFINE
     ↓
04 OSS RESEARCH
     ↓
05 OSS EVALUATION
     ↓
06 ARCHITECTURE / CONTRACT
     ↓
07 FRONTEND + BACKEND DESIGN
     ↓
08 IMPLEMENT VERTICAL SLICE
     ↓
09 INTEGRATE
     ↓
10 TEST
     ↓
11 RUNTIME VERIFY
     ↓
12 SECURITY AUDIT
     ↓
13 PERFORMANCE AUDIT
     ↓
14 UX / ACCESSIBILITY / I18N AUDIT
     ↓
15 FULL REPOSITORY AUDIT
     ↓
16 DOCUMENT
     ↓
17 UPDATE PROJECT STATE
     ↓
18 COMMIT
     ↓
19 REVIEW
     ↓
20 SELECT NEXT WORK UNIT
```

No step is optional merely because a feature appears small. Steps may be combined for genuinely trivial changes, but the required evidence must still exist.

---

# 3. Phase 0 — Project Contract

Before feature development begins:

- establish repository identity;
- establish the master prompt;
- establish this workflow;
- establish architecture principles;
- establish the feature/capability scope;
- establish release gates;
- establish OSS-first rules;
- establish native Windows development rules;
- establish the future Docker production boundary;
- establish project-state and chat-handoff rules;
- establish Git discipline.

### Exit criteria

- master prompt committed;
- workflow committed;
- architecture contract committed;
- project-state mechanism available;
- repository baseline auditable;
- no unexplained generated or empty artifacts.

---

# 4. Phase 1 — Frontend and Backend Foundations

Frontend and backend foundations are established simultaneously.

## Backend foundation

Build and verify:

- configuration/settings;
- application lifecycle;
- dependency ownership;
- API application factory;
- health endpoint;
- real readiness checks;
- database engine/session lifecycle;
- migration foundation;
- Redis lifecycle;
- NATS lifecycle;
- application ports;
- domain boundaries;
- API error model;
- structured error handling;
- logging/observability foundation;
- test infrastructure;
- runtime verification.

## Frontend foundation

Build and verify:

- Next.js application;
- TypeScript foundation;
- route structure;
- terminal shell;
- API client boundary;
- shared contract/type boundary;
- error/loading/empty states;
- responsive shell;
- accessibility foundation;
- RTL/LTR architecture;
- localization boundary;
- PWA foundation where applicable;
- chart integration boundary;
- state ownership rules.

### Parallel contract flow

```text
Backend Contract
       ↕
Shared / Generated Contract
       ↕
Frontend API Client
       ↕
Terminal UI
```

### Exit criteria

- native Windows development works;
- backend boots locally;
- frontend boots locally;
- API contract is consumable by frontend;
- health/readiness behavior is meaningful;
- tests pass;
- no hidden Docker dependency exists;
- documentation is synchronized.

---

# 5. Phase 2 — Market Data

Build the market-data capability as a vertical slice.

## Discovery

Identify required instruments, providers, historical data, realtime feeds, normalization needs, rate limits, failure modes, timestamps, precision, and data quality requirements.

## OSS research

Evaluate mature libraries and components for provider access, parsing, transport, time-series processing, storage, and related functions before writing custom infrastructure.

## Architecture

```text
Provider
   ↓
Adapter
   ↓
Normalized Contract
   ↓
Application / Domain
   ↓
Persistence
   ↓
Events
   ↓
API
   ↓
Frontend Client
   ↓
Terminal
```

## Required concerns

- historical ingestion;
- realtime ingestion;
- normalization;
- deduplication;
- ordering;
- timestamps/time zones;
- gaps;
- retries;
- provider failures;
- rate limits;
- data quality;
- provenance;
- caching;
- persistence;
- event publication;
- frontend subscription/update behavior.

### Exit criteria

A user-visible market-data path works end-to-end and is tested under normal and failure conditions.

---

# 6. Phase 3 — Chart Terminal

The chart terminal is a core product surface, not a decorative frontend component.

## Chart-first architecture

```text
Market Data
    ↓
Chart Data Contract
    ↓
Chart State
    ↓
Chart Engine
    ↓
Overlays / Indicators / Drawings
    ↓
Interaction Layer
    ↓
Terminal Tools
```

When Lightweight Charts is selected, evaluate and use its complete relevant stable capability surface, including as applicable:

- candlestick rendering;
- line/area/histogram series;
- series lifecycle;
- price/time scales;
- markers;
- primitives/extensions supported by the selected stable release;
- drawing/annotation integration where supported or appropriate through an explicit extension layer;
- crosshair interaction;
- realtime updates;
- historical data loading;
- viewport/time-range management;
- multiple series;
- formatting;
- localization;
- resize handling;
- theme integration;
- performance controls;
- cleanup/disposal;
- state synchronization.

Do not assume every library feature belongs in the UI. Evaluate each relevant capability and document the decision.

### Exit criteria

- chart renders real data;
- symbol/timeframe changes work;
- realtime updates work;
- lifecycle cleanup works;
- overlays have explicit ownership;
- performance is measured;
- terminal interaction is keyboard/accessibility-aware;
- RTL/LTR behavior is correct;
- tests cover critical chart state transitions.

---

# 7. Phase 4 — Technical and Structural Analysis

Implement and integrate, as appropriate:

- multiple timeframes;
- Fair Value Gaps;
- Order Blocks;
- market structure;
- liquidity concepts;
- indicators;
- evidence generation;
- evidence normalization;
- confidence/uncertainty representation;
- historical analysis.

Each analytical module must have:

- clear domain contract;
- deterministic rules where applicable;
- provenance;
- timestamping;
- test vectors;
- edge-case behavior;
- integration with chart visualization;
- API representation;
- frontend representation.

---

# 8. Phase 5 — Intelligence / Elyrava

Elyrava is the internal intelligence subsystem.

Build it as a governed pipeline rather than a single opaque model call.

```text
Data
 ↓
Evidence
 ↓
Feature / Context
 ↓
Model / Provider
 ↓
Inference
 ↓
Uncertainty
 ↓
Aggregation
 ↓
Decision Representation
 ↓
Outcome
 ↓
Attribution
 ↓
Calibration
 ↓
Drift
```

Record appropriate provenance and model/provider identity for important intelligence artifacts.

External research must be traceable. Datasets must have provenance and version identity.

Improvement proposals must be sandboxed, tested, evidence-based, reversible, and auditable.

Production promotion must have explicit controls and human approval where required by governance.

---

# 9. Phase 6 — Signals and Decision Workflow

Build:

- evidence aggregation;
- consensus;
- signal generation;
- final decision representation;
- uncertainty;
- explanation/evidence display;
- signal lifecycle;
- realtime delivery;
- notification triggers;
- outcome tracking.

The system should present one clear final trade-oriented answer while retaining the underlying evidence and uncertainty rather than hiding disagreement or ambiguity.

---

# 10. Phase 7 — Backtest and Replay

Build backtesting and replay against the same normalized market contracts used by live workflows wherever practical.

Requirements:

- deterministic datasets;
- versioned strategies/configurations;
- reproducible runs;
- transaction/cost assumptions;
- slippage/spread assumptions where applicable;
- result provenance;
- metrics;
- event timelines;
- replay controls;
- comparison support;
- auditability.

Avoid maintaining separate incompatible logic paths for historical and live analysis unless a real technical boundary requires it.

---

# 11. Phase 8 — Risk and Position Sizing

Build:

- account equity;
- broker account configuration;
- leverage;
- instrument constraints;
- risk percentage/amount;
- stop-loss;
- take-profit;
- position sizing;
- margin considerations;
- validation;
- scenario calculations.

Risk calculations must be deterministic, tested, explainable, and protected against invalid inputs.

---

# 12. Phase 9 — Journal and Outcome Intelligence

Build:

- trading journal;
- signal-to-trade linkage;
- execution context;
- outcome recording;
- attribution;
- calibration;
- drift detection;
- performance analysis.

The objective is to close the loop:

```text
Prediction / Signal
       ↓
Decision
       ↓
Execution / Journal
       ↓
Outcome
       ↓
Attribution
       ↓
Calibration
       ↓
Improvement Evidence
```

---

# 13. Phase 10 — Identity and Access

Build:

- identity model;
- sessions;
- authentication;
- authorization;
- role/permission boundaries;
- Google OAuth;
- account lifecycle;
- admin boundaries;
- audit records.

Security tests must be developed alongside these capabilities.

---

# 14. Phase 11 — Subscription and Crypto Payments

Build the commercial lifecycle explicitly:

```text
Free / Pro
   ↓
Checkout
   ↓
Payment Intent
   ↓
Address / Payment Details
   ↓
Verification
   ↓
Settlement
   ↓
Subscription
   ↓
Entitlement
   ↓
Activation
   ↓
Expiry / Renewal
   ↓
Reconciliation
```

Every transition must be idempotent, auditable, recoverable, and testable.

Free and Pro feature limits must be configuration-driven rather than scattered hardcoded values.

---

# 15. Phase 12 — Notifications

Build notifications around explicit event and preference contracts.

Cover:

- signal notifications;
- lifecycle notifications;
- payment/subscription notifications;
- system notifications;
- user preferences;
- delivery state;
- retries;
- deduplication;
- auditability.

---

# 16. Phase 13 — Research Intelligence and Governance

Build controlled research and improvement workflows.

Requirements:

- source provenance;
- dataset provenance;
- versioning;
- evaluation;
- reproducibility;
- sandbox execution;
- controlled code/model changes;
- proposal queue;
- approval;
- promotion;
- rollback;
- audit trail.

No uncontrolled production mutation.

---

# 17. Phase 14 — Production Engineering

Only after native Windows development and application capabilities are sufficiently mature:

- introduce Docker packaging;
- define production images;
- define Compose or equivalent production orchestration where appropriate;
- configure production secrets externally;
- establish CI/release validation;
- define health/readiness and graceful shutdown behavior;
- establish backups/recovery;
- establish observability;
- establish deployment/rollback procedures;
- perform production security review.

Docker is a production/release concern at this stage, not a prerequisite for normal development.

---

# 18. OSS Selection Workflow

Every significant OSS candidate follows:

```text
DISCOVER
   ↓
LICENSE CHECK
   ↓
MAINTENANCE CHECK
   ↓
RELEASE MATURITY
   ↓
SECURITY REVIEW
   ↓
CAPABILITY INVENTORY
   ↓
INTEGRATION FIT
   ↓
PERFORMANCE FIT
   ↓
BROWSER / RUNTIME FIT
   ↓
OPERABILITY
   ↓
EXTENSIBILITY
   ↓
TOTAL COST / COMPLEXITY
   ↓
SELECT / REJECT
```

For selected components, record:

- project;
- version;
- license;
- purpose;
- capabilities used;
- capabilities intentionally unused;
- integration boundary;
- configuration;
- security considerations;
- performance considerations;
- upgrade policy;
- fallback/exit strategy when practical.

Never add a library without knowing why it exists.

---

# 19. Vertical Slice Rule

A product capability is developed end-to-end rather than layer-by-layer in isolation.

Example:

```text
Requirement
   ↓
Domain Contract
   ↓
Application Use Case
   ↓
Persistence / Event Contract
   ↓
API Contract
   ↓
Frontend Type / Client
   ↓
UI
   ↓
Realtime / Interaction
   ↓
Tests
   ↓
Security
   ↓
Observability
   ↓
Documentation
```

This is the default pattern for all major product modules.

---

# 20. Test Workflow

For each change:

### Unit
Test pure domain and isolated logic.

### Application / Contract
Test use cases, policies, serialization, validation, and boundary behavior.

### Integration
Test database, Redis, NATS, provider adapters, and other real integrations where applicable.

### End-to-End
Test critical user journeys across frontend and backend.

### Runtime
Start the actual application and verify the behavior that tests cannot prove alone.

### Release
Run repository-wide checks and the full release gate.

---

# 21. Error Handling Workflow

Every external or infrastructure boundary must define:

- expected errors;
- retryable errors;
- non-retryable errors;
- timeout behavior;
- cancellation behavior;
- partial failure behavior;
- user-facing error mapping;
- logging/observability;
- audit implications.

Do not catch broad exceptions merely to keep a process alive.

---

# 22. Performance Workflow

Performance work follows:

```text
MEASURE
  ↓
IDENTIFY BOTTLENECK
  ↓
FORM HYPOTHESIS
  ↓
CHANGE
  ↓
MEASURE AGAIN
  ↓
KEEP ONLY IF BENEFICIAL
```

Avoid speculative optimization.

---

# 23. Security Workflow

For every major feature:

1. identify trust boundaries;
2. identify assets;
3. identify attacker-controlled input;
4. validate authorization;
5. validate secrets handling;
6. inspect dependency/supply-chain implications;
7. inspect replay/idempotency behavior;
8. inspect logging for sensitive data;
9. test abuse/failure paths;
10. document residual risks.

---

# 24. Frontend Quality Workflow

For every major frontend module:

- verify TypeScript;
- verify imports;
- verify route behavior;
- verify API contract;
- verify loading/error/empty states;
- verify keyboard operation;
- verify semantic accessibility;
- verify responsive behavior;
- verify RTL/LTR;
- verify localization;
- verify chart/resource cleanup;
- verify performance;
- verify visual consistency.

Core terminal UX must remain chart-first.

---

# 25. Full Repository Audit Workflow

Before a major milestone/release:

```text
Repository tree
    ↓
Imports
    ↓
Tests
    ↓
Configuration
    ↓
Migrations
    ↓
API contracts
    ↓
Frontend routes/components
    ↓
OSS integration
    ↓
Runtime startup
    ↓
Security
    ↓
Performance
    ↓
Observability
    ↓
Documentation
    ↓
Generated/dead/empty files
    ↓
Git diff
```

The audit must include files untouched by the current feature.

---

# 26. Documentation Update Workflow

When a meaningful architectural or implementation decision is made:

1. update the relevant document;
2. update current project state;
3. record rationale;
4. record consequences;
5. record migration/upgrade implications if any;
6. commit documentation with the implementation when practical.

The workflow and master prompt are living documents and must be revised when project reality changes.

---

# 27. Git Commit Workflow

Use coherent commits.

Recommended pattern:

```text
feat: ...
fix: ...
refactor: ...
docs: ...
test: ...
perf: ...
security: ...
chore: ...
```

Before commit:

- inspect diff;
- inspect status;
- run relevant tests;
- run lint/type/build checks applicable to the change;
- verify documentation;
- verify no secrets/generated artifacts;
- verify the change does not introduce unrelated churn.

---

# 28. Chat Handoff Workflow

A new chat is a continuation of the repository state, not a new project.

At the beginning of a new chat, the assistant must inspect:

1. master prompt;
2. development workflow;
3. current project-state documentation;
4. architecture;
5. feature/module status;
6. latest Git commit;
7. relevant source tree;
8. relevant tests;
9. current configuration;
10. outstanding blockers.

Then report:

```text
PHASE
STAGE
DONE
CURRENT
NEXT
BLOCKERS
LATEST COMMIT
USER ACTION
OVERALL PROGRESS
```

The assistant must not ask the user to repeat information already encoded in repository documentation.

---

# 29. Work-Unit Selection Algorithm

When deciding what to implement next:

1. resolve blockers;
2. protect architectural foundations;
3. complete dependencies required by current vertical slices;
4. maintain frontend/backend parallel progress;
5. prioritize end-to-end demonstrable capability;
6. address security and reliability risks early;
7. prefer mature OSS reuse where it reduces custom work;
8. avoid speculative features that do not unlock a real requirement;
9. keep documentation synchronized;
10. leave the repository in a runnable state.

---

# 30. Stop Conditions

Stop and reassess instead of continuing blindly when:

- the requested behavior conflicts with an existing contract;
- a dependency is immature or incompatible;
- the database state is ambiguous;
- data loss is possible;
- an external provider contract is uncertain;
- an OSS license is unclear;
- a security boundary is unclear;
- a test failure may indicate a deeper regression;
- a generated artifact obscures the source of truth;
- the repository contains unexplained state;
- the next change would require speculative architecture.

The correct response is investigation, not accumulation of patches.

---

# 31. Project State Template

At every meaningful milestone maintain this information:

```text
PROJECT: CFIP-PRO
PHASE: <current phase>
STAGE: <current stage>
OVERALL_PROGRESS: <evidence-based percentage>

DONE:
- ...

CURRENT:
- ...

NEXT:
- ...

BLOCKERS:
- ...

LATEST_COMMIT:
- ...

OSS_SELECTED:
- ...

OSS_PENDING:
- ...

TEST_STATUS:
- ...

SECURITY_STATUS:
- ...

PERFORMANCE_STATUS:
- ...

DOCUMENTATION_STATUS:
- ...

USER_ACTION:
- ...
```

Never claim a feature is complete when important verification remains.

---

# 32. Final Release Gate

A release candidate may proceed only when:

- all committed requirements for the release are implemented;
- frontend/backend contracts are synchronized;
- relevant OSS integrations are verified;
- tests pass;
- runtime verification passes;
- migrations are valid;
- security review passes;
- performance review is acceptable;
- accessibility/i18n review is acceptable;
- observability is adequate;
- configuration is safe;
- documentation is current;
- stale references are removed;
- empty/dead/unnecessary files are reviewed;
- Git diff is clean and understood;
- release state is recorded.

---

# 33. Non-Negotiable Principles

1. Greenfield architecture.
2. `CForex` is the sole reference capability source.
3. OSS-first.
4. Maximum responsible reuse.
5. Full relevant capability utilization of selected OSS.
6. Minimum unnecessary custom code.
7. Native Windows development now.
8. Docker production later.
9. Frontend and backend in parallel.
10. Vertical slices over disconnected layers.
11. Contracts are first-class.
12. Security from the beginning.
13. Tests accompany implementation.
14. Evidence before optimization.
15. No destructive operation without verification.
16. No unnecessary downloads/builds/dependencies.
17. Living documentation.
18. Repository state is the source of continuity between chats.
19. Every meaningful change ends with audit, documentation, and coherent commit.
20. Never sacrifice correctness and long-term maintainability for a superficially fast patch.
