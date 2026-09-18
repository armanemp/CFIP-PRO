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

This is model-agnostic. A later trainer can consume these examples using a separately governed ML dataset/model registry without coupling the core platform to a specific vendor.

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
