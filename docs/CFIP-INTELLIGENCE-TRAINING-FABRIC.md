# CFIP-PRO — Platform Intelligence & Training Fabric

## Purpose

CFIP intelligence is a governed evidence-and-learning system, not an unconstrained LLM loop.

The deterministic market-analysis engine remains the canonical source for market state, confluence gates and trade recommendations. AI components may explain, synthesize, research, discover hypotheses and propose changes, but cannot silently override a deterministic safety or trading gate.

## Learning planes

1. Market learning — structure, FVG, order blocks, liquidity, regimes, volatility and MTF context.
2. Outcome learning — signal/trade outcomes, excursion, target/stop results and delayed labels.
3. User learning — explicit feedback, journal annotations and corrections.
4. Research learning — externally sourced evidence with freshness, provenance and content hashes.
5. Data-quality learning — provider gaps, anomalies, stale feeds and schema violations.
6. Model learning — evaluation results, calibration, drift and failure clusters.
7. Risk learning — sizing/target outcomes and broker execution constraints.
8. System learning — incidents, performance regressions and reliability patterns.

## Learning lifecycle

observe → normalize → validate → provenance → feature/lesson extraction → candidate → validation → approval → training/evaluation → promotion → monitor → rollback/supersede

No candidate becomes production knowledge merely because an LLM generated it.

## Training-example contract

TrainingPreparationService converts the canonical UnifiedAnalysisRead plus auditable evidence into a deterministic TrainingExample. The feature vector currently captures analysis score/confidence, confluence score, trend/momentum/structure scores, FVG and order-block counts, liquidity-pool count, and aligned MTF count.

Training examples remain model-agnostic. `DatasetManifest` now provides an immutable dataset-level lineage boundary: unique example IDs and source hashes, an explicit feature schema, a label policy and a provenance hash are required. This keeps future trainers decoupled from the core platform while making dataset identity and reproducibility explicit.

## Evidence requirements

Learning that claims market, outcome, research or model knowledge requires evidence IDs. Evidence carries source, observation time, content hash, freshness and confidence.

This enables reproducible datasets, stale-evidence rejection, provenance chains, deduplication, audit/replay, dataset versioning and eventual knowledge-graph links.

## AI safety boundary

AI may summarize canonical evidence, identify conflicts, formulate hypotheses, research, propose thresholds/strategies/workflows, generate code in a sandbox, and produce explanations/coaching.

AI may not silently change trading gates, promote code, change risk limits, fabricate market facts, convert stale research into current evidence, or turn an unvalidated hypothesis into a production lesson.

Proposals therefore carry evidence IDs, risk level and explicit approval state.

## Next intelligence layers

- persistent evidence/lesson/proposal stores
- dataset manifests and immutable versions
- feature lineage
- outcome attribution
- calibration and drift metrics
- model registry/evaluation
- research freshness gates
- sandbox execution/evidence capture
- governed proposal queue
- knowledge graph and semantic retrieval
- agent orchestration with deterministic tool permissions
- human approval and rollback

## Self-healing and self-development fabric

CFIP treats self-healing as a controlled engineering feedback loop rather than unrestricted autonomous mutation:

1. Observe health, correctness, latency, data-quality and security signals.
2. Correlate signals with deployment, dependency, market-data and runtime evidence.
3. Produce an explicit diagnosis with confidence, evidence and blast radius.
4. Generate a reversible repair proposal with expected effect and rollback plan.
5. Run tests, static analysis, security checks and targeted regression verification in isolation.
6. Require approval for changes outside a narrowly configured reversible local/component policy.
7. Apply through an auditable release boundary, monitor the canary, and automatically roll back when protected health invariants fail.
8. Record the outcome as system learning so repeated incidents become less likely.

Self-development follows the same boundary. The intelligence layer can research the codebase, identify architecture or quality gaps, generate candidate changes, build experiments and collect evidence. It cannot silently mutate production code or production risk controls. Every promoted change must have provenance, tests, verification evidence, a rollback path and an auditable lifecycle.

### Protected health invariants

The self-healing execution boundary now has a deterministic `HealthInvariantReport`. The protected snapshot covers tests, security, market-data quality/freshness, risk-gate integrity, audit-chain integrity and authorization integrity. Missing observations are treated as blocking rather than healthy, and duplicate observations are rejected. Repair and self-development gates can consume this report and refuse promotion whenever a protected invariant is failing.

This is intentionally an evaluation contract, not an executor. A future runtime health collector must provide signed/auditable evidence to it; the evaluator itself performs no process, shell, network or production mutation.

### Autonomous improvement domains

- code quality and type safety
- test-gap discovery and regression generation
- dependency/security advisories
- performance and resource optimization
- database/index/cache tuning proposals
- data-provider quality and fallback proposals
- frontend accessibility/performance proposals
- prompt/tool policy evaluation
- model evaluation, calibration and drift
- documentation consistency
- architecture-debt detection
- operational runbook improvement

### Non-negotiable invariants

Self-healing must never bypass authentication/authorization, data provenance, risk limits, deterministic trading gates, audit logging, or release verification. Destructive or platform-wide changes remain approval-gated.

## Implemented governed outcome fabric

The repository now includes a deterministic signal/outcome boundary in `cfip.domain.outcomes` and `cfip.application.outcomes`. Signal IDs are deterministic from symbol/timeframe/direction/decision time, emission enforces startup suppression and cooldown/spacing, and outcome labeling filters observations by event time so pre-decision candles cannot leak into labels. Calibration exposes Brier score, log loss and expected calibration error with explicit binning; drift detection is sample-gated and reports a standardized mean-shift signal rather than silently changing a model.

## Implemented self-healing execution boundary

Self-healing is now split into diagnosis/proposal and execution authorization. `self_healing_execution` defines artifact digests, rollback digests, safety invariants, test evidence, canary requirements, approval boundaries, repair budgets and cooldown/circuit-breaker controls. The application service is a pure policy gate: it does not execute shell commands or mutate production. Code changes require explicit approval; self-development remains branch/sandbox-oriented and production application is denied unless an explicit production grant exists. Before execution, an optional health-invariant report can now add a hard blocking reason for every failed or missing protected invariant.

## Required next hardening layers

Before autonomous execution is enabled, the platform still needs persistent event/audit storage, cryptographic artifact/signature verification, isolated worktrees/containers, a restricted action registry, a runtime health collector feeding the invariant evaluator, canary telemetry, automatic rollback, incident locking/escalation, dependency/security/test-generation agents, durable dataset manifests and model evaluation/registry. These are release gates, not optional AI features.
