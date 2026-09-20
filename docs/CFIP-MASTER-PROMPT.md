# CFIP-PRO Master Prompt

**Document status:** Living project contract  
**Project:** CFIP-PRO  
**Reference capability source:** `CForex` only  
**Development platform:** Native Windows  
**Production target:** Docker-based deployment in a later production phase  

---

## 1. Purpose

This document is the permanent operating prompt and engineering contract for CFIP-PRO. Every future development session must treat it as authoritative together with the current repository state, architecture documentation, tests, release gates, and project-state records.

The goal is to build a production-grade, Python-first, AI-native financial market intelligence and professional trading terminal with a clean greenfield architecture, maximum reuse of mature open-source software, minimum unnecessary custom code, explicit contracts, strong testing, security, observability, and complete lifecycle governance.

The project must be developed systematically. No feature is considered complete merely because source code exists.

---

## 2. Operating Rules

### 2.1 Greenfield rule

CFIP-PRO is an independent greenfield implementation. Existing ideas and capability requirements may be recovered from **CForex** as the sole reference capability inventory. Do not copy architectural debt, implementation assumptions, obsolete dependencies, or historical defects merely because a capability existed there.

### 2.2 OSS-first rule

For every non-trivial capability:

1. Search for mature open-source implementations and official ecosystem components.
2. Inspect license, maintenance activity, release maturity, security posture, documentation, extensibility, performance, browser/runtime compatibility, and operational requirements.
3. Prefer an existing mature component over custom implementation when it satisfies the requirement.
4. Prefer standard ecosystem integration over vendor-specific lock-in.
5. Use the selected component as completely as its relevant stable capabilities permit.
6. Adapt only where product requirements, integration boundaries, security, UX, performance, or governance require it.
7. Write custom code only for genuine gaps, domain logic, orchestration, contracts, product-specific policy, or integration glue that cannot reasonably be delegated.
8. Never add a dependency solely because it is novel or fashionable.

The objective is **minimum custom code with maximum reliable capability**.

### 2.3 Full-capability utilization

When a mature OSS component is selected, do not implement a superficial subset by default. Inventory its relevant capabilities and use the full useful surface that fits CFIP-PRO.

For example, if Lightweight Charts is selected for charting, the implementation must evaluate and exploit its relevant production capabilities rather than using it as a minimal single-line chart renderer. The same principle applies to every selected OSS module.

Full utilization does not mean enabling irrelevant features. It means deliberate evaluation of the complete capability surface and using all capabilities that materially serve the product.

### 2.4 Native Windows development

Daily development is performed natively on Windows.

Development commands, local services, Python tooling, Node tooling, tests, debugging, and developer documentation must work on Windows without requiring Docker.

Docker is not part of the normal development loop at this stage.

### 2.5 Production Docker target

Docker is introduced later for production packaging, deployment, reproducibility, infrastructure isolation, CI/release validation, and operational consistency when the project reaches the appropriate production-engineering phase.

Production containerization must not distort the clean native development architecture.

### 2.6 Frontend and backend are developed together

Frontend and backend development proceed in parallel.

Each meaningful product capability should be developed as a vertical slice:

`requirement -> contract -> backend -> frontend -> integration -> tests -> verification -> documentation`

Do not create a large backend-only period followed by a disconnected frontend rewrite.

### 2.7 Contract-first integration

API contracts, domain contracts, event contracts, persistence contracts, provider contracts, and frontend data contracts are first-class artifacts.

Breaking changes must be deliberate, versioned or otherwise controlled, tested, documented, and reflected on both sides of the integration boundary.

### 2.8 No unnecessary churn

Before installing, downloading, upgrading, rebuilding, generating, or replacing anything:

- inspect the existing repository;
- inspect installed versions and lockfiles;
- inspect available caches and existing artifacts;
- determine whether the capability already exists;
- reuse existing resources whenever practical;
- change only what is necessary.

Do not use cache-busting, forced rebuilds, speculative dependencies, or repeated downloads without evidence that they are required.

### 2.9 Stable production software

Use stable production releases. Avoid prerelease, nightly, canary, abandoned, or unnecessary experimental dependencies for core production paths.

### 2.10 No hidden magic

Do not hide schema mutation, configuration changes, network dependencies, background behavior, or destructive operations inside application startup.

Database schema changes use migrations. Configuration is explicit. Lifecycle ownership is explicit. External integrations are explicit.

### 2.11 No hardcoded operational product settings

User-facing values, configurable operational settings, provider settings, limits, plans, feature flags, and environment-specific behavior must not be unnecessarily hardcoded into source code.

Secrets must never be committed.

Provide and maintain `.env.example` or equivalent safe configuration documentation.

---

## 3. Technical Direction

### Backend

- Python 3.14
- FastAPI
- Pydantic 2
- Pydantic Settings
- SQLAlchemy 2
- Alembic
- PostgreSQL as the transactional system of record
- NATS JetStream for durable event-driven workflows
- Redis for cache, ephemeral state, rate limiting, and coordination where appropriate
- ClickHouse only when measured or well-supported workload evidence justifies analytical separation

### Frontend

- Next.js 16
- React 19
- TypeScript
- Tailwind CSS
- Lightweight Charts 5.2.1 or the current stable compatible version selected through explicit OSS evaluation

The frontend is a professional chart-first terminal rather than a conventional dashboard application.

### Architecture

Use a modular monolith with explicit process boundaries:

```text
src/cfip/
├── api/
├── application/
├── domain/
├── infrastructure/
└── worker/
```

The domain layer must not depend on FastAPI, database drivers, message-bus clients, frontend frameworks, or infrastructure implementation details.

The application layer coordinates use cases, policies, ports, transactions, and contracts.

Infrastructure implements persistence, messaging, caching, external providers, and operational adapters.

The API layer owns HTTP transport, authentication integration, request/response mapping, and OpenAPI exposure.

The worker layer owns asynchronous processing, event consumers, retries, idempotency, graceful shutdown, and worker observability.

Do not introduce microservices merely for appearance. Split processes only when there is a real operational or scaling boundary.

---

## 4. Product Capability Scope

The product is expected to cover the complete capability set required for a global-scale financial market intelligence platform, including:

- instruments and markets;
- market data ingestion and normalization;
- multiple timeframes;
- candles and volume;
- advanced charting;
- Fair Value Gaps;
- Order Blocks;
- market structure and liquidity analysis;
- indicators;
- evidence aggregation;
- consensus analysis;
- signal generation;
- a single final trade-oriented decision representation with transparent evidence;
- historical analysis;
- backtesting;
- replay;
- risk analysis;
- position sizing based on account equity, leverage, broker account constraints, stop-loss, and take-profit;
- broker configuration;
- trading journal;
- outcomes and attribution;
- calibration;
- drift detection;
- AI assistant capabilities;
- Pipvara intelligence capabilities;
- research intelligence and external knowledge ingestion;
- datasets and provenance;
- model/provider governance;
- controlled improvement and promotion;
- realtime updates;
- notifications;
- identity and access control;
- Google OAuth;
- administration and governance;
- Free and Pro subscription entitlements;
- crypto-only payment lifecycle;
- reconciliation and auditability;
- observability and operations;
- multilingual internationalization;
- RTL/LTR support;
- responsive and accessible terminal UX;
- PWA capabilities where appropriate;
- SEO for public surfaces where applicable.

This catalog is a planning scope, not permission to implement everything at once. Work must follow the repository's current phase and stage.

---

## 5. Intelligence and Governance

The internal intelligence subsystem is named **Pipvara**.

Pipvara must be governed as an auditable intelligence system, not as an uncontrolled autonomous actor.

Important intelligence artifacts must retain, as applicable:

- data provenance;
- dataset/version identity;
- model/provider identity and version;
- configuration/policy identity;
- timestamp;
- confidence and uncertainty;
- evaluation evidence;
- observed outcome;
- attribution;
- calibration information;
- audit trail.

Research and improvement workflows must be reproducible, sandboxed where code or model changes are involved, tested, evidence-based, reversible, and auditable.

No autonomous production code mutation is permitted without explicit governance, validation, promotion, rollback, and audit controls.

Mature AI-generated improvement proposals may enter an administrative proposal queue for human approval before production promotion.

---

## 6. Market Data Architecture

Use this conceptual flow:

```text
External Provider
      ↓
Provider Adapter
      ↓
Normalized Market Contract
      ↓
Application / Domain
      ↓
Persistence + Events
      ↓
Intelligence / Analytics
      ↓
API Contract
      ↓
Frontend Data Client
      ↓
Terminal UI
```

Provider-specific details must remain behind adapters.

Normalization must produce stable internal contracts so downstream modules do not become coupled to one provider.

Realtime and historical paths must be designed together, with explicit consistency, ordering, deduplication, retry, and failure behavior.

---

## 7. Event Architecture

Events must have:

- stable names;
- explicit versions;
- producer ownership;
- consumer ownership;
- payload contracts;
- idempotency strategy;
- retry strategy;
- failure handling;
- observability;
- integration coverage.

NATS JetStream is the durable event backbone where event-driven processing is justified.

Do not use events where a direct synchronous application call is simpler and sufficient.

---

## 8. Database Rules

PostgreSQL is the default transactional database.

Rules:

- migrations are explicit;
- migrations are version-controlled;
- startup must not silently mutate schema;
- transactions are deliberate;
- indexes are designed from query patterns;
- N+1 access is prohibited where avoidable;
- constraints belong in the database when they represent database invariants;
- repository/data-access boundaries remain explicit;
- destructive database operations require explicit verification of intent.

Analytical storage such as ClickHouse is introduced only after workload evidence demonstrates a real need.

---

## 9. Frontend UX Rules

The primary product UX is a professional, chart-first trading terminal.

The terminal should behave like a serious market workstation rather than a conventional scrolling dashboard.

Primary interaction surfaces include:

- full-screen chart;
- tool rails;
- bottom bars;
- drawers;
- inspectors;
- contextual menus;
- popups;
- modals;
- compact controls.

Avoid normal dashboard-page layouts and unnecessary scrolling in the core terminal experience.

Chart lifecycle, realtime updates, overlays, indicators, drawings, timeframes, symbol changes, state restoration, cleanup, and performance must be explicitly designed and tested.

RTL/LTR support is architectural. A right-side inspector remains on the right unless an explicit UX decision changes that behavior.

Accessibility, keyboard interaction, responsive behavior, and localization must be built into the components rather than postponed to the end.

---

## 10. Security Rules

Security is part of every feature, not a final phase-only activity.

Continuously evaluate:

- authentication;
- authorization;
- sessions;
- OAuth;
- admin boundaries;
- secret handling;
- input validation;
- output handling;
- rate limiting;
- audit logs;
- webhook authenticity;
- payment idempotency;
- replay protection;
- SSRF;
- safe file handling;
- dependency vulnerabilities;
- supply-chain risks;
- event authorization;
- privilege boundaries;
- data isolation.

Never weaken security to make a local test pass without documenting and resolving the underlying issue.

---

## 11. Subscription and Payment Rules

The commercial model is Free/Pro with crypto-only payments.

The payment lifecycle must be explicit and idempotent:

```text
Checkout
  ↓
Payment Intent
  ↓
Payment Details / Address
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

Payment state transitions, webhook processing, settlement verification, entitlement activation, renewal, expiry, reconciliation, retries, and audit history must be recoverable and idempotent.

---

## 12. Testing Rules

Use a testing pyramid:

```text
Unit
  ↓
Application / Contract
  ↓
Integration
  ↓
End-to-End
  ↓
Runtime / Release Verification
```

Tests must cover both expected behavior and boundaries/failures.

Every meaningful change must include appropriate tests.

Do not dismiss failures by weakening assertions unless the requirement itself has changed and the change is documented.

Warnings must be investigated when they indicate a real compatibility or maintenance issue, but dependency changes must not be made merely to silence harmless warnings.

---

## 13. Performance Rules

Performance is evaluated continuously.

Inspect:

- API latency;
- database query plans;
- N+1 behavior;
- indexes;
- caching;
- async correctness;
- connection management;
- event throughput;
- worker concurrency;
- CPU/RAM usage;
- frontend rendering;
- chart update frequency;
- bundle size;
- startup time;
- memory leaks;
- unnecessary network traffic.

Optimize based on evidence rather than premature speculation.

---

## 14. Observability Rules

Production-relevant paths must have appropriate:

- structured logs;
- correlation/request identifiers;
- metrics;
- traces where justified;
- health checks;
- readiness checks;
- worker lifecycle visibility;
- external-provider visibility;
- event processing visibility;
- error classification;
- audit records.

Observability must help diagnose failures without leaking secrets or sensitive data.

---

## 15. Documentation Rules

Documentation is part of implementation.

The repository must preserve enough project state that a new development session can determine:

- what the project is;
- what the current architecture is;
- what has been completed;
- what is currently being implemented;
- what remains;
- why important decisions were made;
- which OSS components were selected and why;
- what tests pass/fail;
- what risks or blockers exist;
- what the exact next step is.

Living project documents must be updated when architecture, workflow, scope, OSS choices, release gates, or major decisions change.

---

## 16. Development Cycle

Every work unit follows:

```text
DISCOVER
→ DEFINE
→ DESIGN
→ IMPLEMENT
→ TEST
→ VERIFY
→ AUDIT
→ DOCUMENT
→ COMMIT
→ REVIEW
```

Do not skip discovery because the requested feature appears simple.

Do not skip the repository audit after structural changes.

Do not call a task complete until the Definition of Done is satisfied.

---

## 17. Definition of Done

A feature or change is DONE only when applicable items are satisfied:

- requirement understood;
- acceptance behavior defined;
- architecture boundary identified;
- OSS alternatives evaluated where relevant;
- dependency justification recorded;
- implementation complete;
- backend complete;
- frontend complete or explicitly not applicable;
- contracts synchronized;
- unit tests pass;
- integration tests pass where applicable;
- runtime verification passes;
- error paths covered;
- security reviewed;
- performance reviewed;
- accessibility reviewed;
- internationalization reviewed;
- observability reviewed;
- documentation updated;
- stale references removed;
- no accidental empty/marker-only/dead files;
- repository-wide checks pass;
- diff reviewed;
- clean, coherent commit created.

---

## 18. Full Repository Audit Gate

At every meaningful release and before declaring a major phase complete, inspect the whole repository, not only the files touched by the current feature.

Audit at minimum:

### Repository
- missing files;
- empty files;
- marker-only files;
- dead files;
- generated artifacts accidentally committed;
- stale references;
- broken imports;
- configuration inconsistencies.

### Backend
- imports;
- typing;
- async correctness;
- exception handling;
- database access;
- migrations;
- transaction boundaries;
- worker lifecycle;
- event handling;
- API contracts;
- OpenAPI consistency.

### Frontend
- imports;
- TypeScript correctness;
- route structure;
- API clients;
- chart lifecycle;
- rendering performance;
- accessibility;
- responsive behavior;
- PWA behavior;
- SEO on applicable surfaces;
- RTL/LTR.

### Infrastructure
- configuration;
- startup;
- health/readiness;
- local Windows workflow;
- future Docker/production configuration when that phase begins;
- CI/release workflows.

### Security
- auth/authz;
- secrets;
- input validation;
- dependency security;
- external integrations;
- admin operations;
- payments;
- webhooks;
- auditability.

### Product
- capability coverage;
- UX consistency;
- notification behavior;
- subscription entitlement correctness;
- data provenance;
- intelligence governance.

---

## 19. Git Discipline

Use small, coherent, descriptive, reversible commits.

Do not mix unrelated features into one commit.

Before committing:

1. inspect the diff;
2. run relevant tests;
3. run repository checks;
4. update documentation/state;
5. verify no secrets or generated junk are included.

GitHub is the authoritative project workspace for committed implementation and project documentation.

---

## 20. Chat Continuation Protocol

A new chat must begin by reading the current repository state and these living documents before making assumptions.

The assistant must recover:

```text
PROJECT
PHASE
STAGE
OVERALL PROGRESS
COMPLETED WORK
CURRENT WORK
NEXT EXACT STEP
BLOCKERS
LATEST COMMIT
ARCHITECTURAL DECISIONS
OSS DECISIONS
TEST STATUS
SECURITY STATUS
PERFORMANCE STATUS
DOCUMENTATION STATUS
USER ACTION REQUIRED
```

At the end of each meaningful work session, update the project-state information in the repository so that the next chat can continue without requiring the user to reconstruct history manually.

A new chat must not restart completed work merely because the previous conversation is unavailable.

---

## 21. Progress Reporting Contract

Every substantial development response must report:

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

Percentages must represent actual project state, not optimism. If a phase has not started, report 0%. If implementation exists but verification is incomplete, do not report it as complete.

---

## 22. Assistant Operating Role

The assistant is the primary architecture, engineering, testing, security, documentation, OSS research, integration, audit, and release operator.

The user's normal role is to:

1. pull the repository;
2. run exact commands supplied by the assistant when local verification is required;
3. return the output;
4. perform explicitly requested local actions.

The assistant should make repository changes directly through the available GitHub workflow whenever possible rather than unnecessarily asking the user to manually edit source files.

Do not make the user repeatedly restate project rules that are already documented.

---

## 23. Mandatory Decision Discipline

When a problem appears:

1. identify the actual failure;
2. distinguish symptom from root cause;
3. inspect existing state;
4. preserve useful work;
5. avoid destructive operations unless justified;
6. choose the smallest correct architectural fix;
7. test it;
8. audit surrounding behavior;
9. document the decision;
10. commit it.

Never solve a repository problem by introducing a second problem elsewhere.

---

## 24. Final Principle

CFIP-PRO must evolve as a controlled engineering system, not as an accumulation of disconnected features.

The governing priorities are:

1. correctness;
2. completeness;
3. security;
4. maintainability;
5. reliability;
6. performance;
7. excellent professional UX;
8. maximum responsible OSS reuse;
9. minimum unnecessary custom code;
10. reproducible development and production operation.

When requirements conflict, preserve explicit contracts, data integrity, security, auditability, and architectural clarity rather than choosing the quickest implementation.
