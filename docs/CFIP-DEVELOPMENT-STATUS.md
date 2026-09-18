# CFIP-PRO Development Status

Updated: 2026-09-18

## Repository rule

CFIP-PRO is an independent platform. CForex is a capability/reference source only. The abandoned `cforex-platform` repository is not an architecture or migration target.

## Implemented intelligence foundations

- Canonical backend analysis boundary with closed-bar causal semantics.
- Event-time multi-timeframe aggregation with completeness reporting.
- FVG lifecycle and order-block lifecycle contracts.
- Liquidity-pool and sweep state.
- Governed intelligence evidence, learning records, proposals and training examples.
- Deterministic signal IDs and signal-emission lifecycle gates.
- Causal outcome labeling with evidence linkage.
- Brier score, log loss and expected calibration error.
- Sample-gated drift detection.
- Self-healing diagnosis/proposal contracts.
- Self-healing execution authorization boundary with artifact, rollback, evidence, approval, canary, budget and cooldown gates.
- Self-development change boundary that cannot apply to production without explicit authorization.

## Safety architecture

The intelligence layer must remain subordinate to deterministic platform invariants. AI may observe, research, diagnose, generate candidates and learn from verified outcomes, but it must not bypass authentication/authorization, provenance, risk controls, audit, release verification or rollback requirements.

Self-healing execution is intentionally separated from diagnosis. No shell/process execution is embedded in the domain policy layer.

## Active completion tracks

1. Persistent signal/outcome/event store and immutable audit trail (repository/API wiring now present; migration chain includes 0003 outcome analytics and 0004 feedback idempotency).
2. Full risk/position-sizing and Entry/SL/TP1/TP2/TP3 engine.
3. Complete FVG/OB/liquidity invalidation, mitigation, breaker and target semantics.
4. Replay/backtest with explicit event-time/no-lookahead contracts.
5. Notification lifecycle with idempotency and cooldown.
6. Dataset manifests, lineage, leakage checks, evaluation and model registry.
7. Sandboxed self-development worktrees, test/security evidence, canary and rollback executor.
8. Health registry, incident circuit breaker and escalation.
9. Research freshness/provenance and knowledge graph/retrieval.
10. Frontend terminal performance, accessibility, i18n/RTL and overlay synchronization audit.
11. Full repository security, dependency, typing, test, runtime, performance and documentation audit.

This file is a living release-gate document. A feature is not considered complete merely because its contract exists; executable integration, persistence, verification and operational safeguards are required before production completion.


## 2026-09-18 — intelligence persistence closure

The governed intelligence boundary is now connected to PostgreSQL rather than remaining contract-only:
- Evidence, learning records, proposals and reviewer feedback have persistent SQLAlchemy models.
- Intelligence feedback is transactional and updates learning validation state.
- An immutable-by-API audit-event table provides idempotent event keys for governed learning actions.
- Alembic migration 0002_intelligence owns the schema change.
- /intelligence/snapshot now persists supplied artifacts and reads authoritative counts from PostgreSQL.
- /intelligence/feedback now persists the review and audit event.
- Duplicate artifact IDs are conflict-checked instead of silently overwritten.
- The learning layer remains prohibited from silently promoting models or bypassing deterministic analysis/risk gates.

### Completion rule

A contract is not considered complete until it has an executable application boundary, persistence where required, migration, idempotency/conflict handling, auditability, tests, and operational verification. The same rule applies to the remaining signal/outcome, risk, replay, notification, research, training, and self-development tracks.


## 2026-09-18 — risk foundation

A deterministic broker-aware risk/target boundary is now present. It requires explicit account equity/risk/leverage, instrument pip/tick/minimum constraints, and quote-to-account conversion before producing sizing. It calculates ATR/min-stop constrained SL, TP1/TP2/TP3, risk amount, quantity-step sizing and margin requirement. If required context is absent or the risk budget cannot satisfy the broker minimum quantity, the result is explicitly unavailable rather than fabricated.


## 2026-09-18 — repair of previously applied intelligence/training gaps

A source-level audit found two concrete integration defects that could survive a contract-only review:
- Training preparation referenced `AnalysisEvidence.id`, while the canonical field is `module`. The lookup is now centralized and tested.
- Training examples require a non-null causal `closed_bar_time`; preparation now rejects missing timestamps instead of constructing an invalid lineage record.

The audit also found that signal/outcome SQLAlchemy models had been present without a corresponding Alembic migration. Migration `0003_outcomes` now creates the signal lifecycle, outcome-event, outcome, calibration and drift tables plus their required indexes.

## 2026-09-18 — health and self-healing control loop foundation

The self-healing layer now has executable deterministic health controls in addition to proposal/execution contracts:
- component health captures status, latency, error rate, freshness, invariant failures and evidence;
- incidents have explicit lifecycle/severity and causal timestamps;
- circuit breakers enforce failure budgets and cooldown-based probing;
- success resets failure state;
- health/circuit transitions are deterministic and covered by unit tests.

The executor boundary remains separate: these controls decide whether remediation may proceed; they do not execute arbitrary shell/process commands.


## 2026-09-18 — release-integrity gates and repair hardening

A second repository-wide source audit closed additional failure modes that had previously allowed commits to land without proving operational integrity:
- Signal lifecycle contracts now enforce causal event ordering and state-dependent timestamps.
- Outcome observations validate OHLC consistency before attribution.
- Training examples now carry schema/version, closed-bar timestamp, feature schema, label horizon and provenance hash; feature/schema mismatches are rejected.
- Persistent signal/outcome storage has deterministic uniqueness boundaries and foreign-key protection.
- Self-healing execution now requires security evidence for mutable artifacts and has an explicit failure circuit-breaker in addition to repair budget/cooldown controls.
- Health remediation preserves the configured circuit cooldown and does not report an already-open circuit as closed.
- CI now starts PostgreSQL, executes the complete Alembic migration chain, runs backend tests/lint, and uses npm ci with the lockfile for the frontend.
- The migration name in this status document is aligned with the actual 0003_signal_outcomes revision.

Verification policy: GitHub commit existence is verified after each write. GitHub currently exposes no workflow/status result for the newest connector-created commits, so CI green is not claimed until an actual GitHub Actions result exists.


## 2026-09-18 — deep causal/outcome and terminal hardening

The latest repository audit applied and verified the following corrections on GitHub:
- Signal/outcome persistence now has an executable repository and FastAPI boundary for lifecycle records, outcome events, finalized outcomes, calibration and drift reports.
- Signal writes preserve risk/target context and evidence on idempotent retries while rejecting conflicting immutable identity fields.
- Intelligence feedback is idempotent at both repository and PostgreSQL-constraint levels; the tamper-evident audit chain serializes chain-head selection inside a PostgreSQL transaction.
- Outcome attribution now marks a same-bar stop/TP collision as 'ambiguous' instead of inventing an event order from OHLC data alone.
- Risk contracts reject non-finite account, instrument, target and output values and require strictly positive target R multiples.
- Higher-timeframe aggregation rejects missing base bars instead of treating sparse data as complete; reported HTF closed-bar time is causal to the last constituent base bar.
- FVG lifecycle now distinguishes partial/mitigated states from invalidation by a close through the zone boundary.
- Chart overlays subscribe to viewport changes so drawings/zones are recalculated after pan/zoom rather than remaining visually stale.
- The frontend chart instance no longer gets recreated merely because locale/grid preferences change; grid updates are applied in place.

The remaining blocker is operational verification: the GitHub connector currently exposes no workflow result for the newest commits, so source correctness has been hardened but CI green is not asserted without an actual Actions result.
