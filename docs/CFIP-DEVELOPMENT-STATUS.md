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

1. Persistent signal/outcome/event store and immutable audit trail.
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
