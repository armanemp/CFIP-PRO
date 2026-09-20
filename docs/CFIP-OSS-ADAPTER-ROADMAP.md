# CFIP-PRO OSS Adapter Roadmap

## Principle

OSS projects provide capabilities; CFIP owns product contracts, evidence, risk semantics,
provenance, governance and user experience. Vendor objects must never leak through the
application/domain boundary.

## Capability map

| Capability | Preferred OSS candidates | CFIP boundary | State |
|---|---|---|---|
| Charting | Lightweight Charts | chart rendering/state | Integrated |
| Technical indicators | TA-Lib | indicator registry | Integrated |
| FVG | pyvsmc | analysis detector contract | Integrated |
| Market data / exchanges | CCXT | MarketDataAdapter | Integrated |
| Dataframes | Polars | research/data substrate | Integrated |
| Agent runtime | PydanticAI | agent/tool/model contracts | Integrated |
| Trading/backtest | NautilusTrader | TradingEngineAdapter | Adapter implemented; runtime benchmark pending |
| ML lifecycle | MLflow | ModelRegistryAdapter | Adapter implemented; registry integration test pending |
| Retrieval | Qdrant / pgvector | RetrievalIndexAdapter | Qdrant adapter implemented; hybrid benchmark pending |
| Observability | OpenTelemetry | ObservabilityAdapter | Adapter implemented; exporter/runtime benchmark pending |
| Security scanning | Bandit / pip-audit / Semgrep / OWASP ZAP | SecurityScannerAdapter | Contract added; integration gated |
| Artifact integrity | Sigstore / cryptographic verification tooling | ArtifactVerifier | SHA-256 verification integrated; signature/key-management gated |
| Research/data | OpenBB | ResearchRetriever | Adapter implemented; provider benchmark pending |
| Agent orchestration | LangGraph / Haystack | MIOS agent contract | Benchmark |
| Feature store | Feast | training feature contract | Benchmark |
| Dataset versioning | DVC | dataset manifest contract | Benchmark |
| Drift/evaluation | Evidently | evaluation/drift contract | Benchmark |
| FIX execution | QuickFIX | execution adapter | Benchmark |
| Quant research | Qlib | research/backtest adapter | Benchmark |

## Integration gates

Every candidate must pass license and maintenance review, runtime compatibility,
semantic equivalence, deterministic tests, performance/resource benchmarks, security
and provenance review, failure/cancellation behavior, rollback/uninstall validation,
and documentation/ownership review.

Installation alone never marks a capability complete. An adapter can be implemented
while remaining gated until its runtime and resource evidence is recorded.

## Selected OSS roles

NautilusTrader is the primary candidate for deterministic research/backtest/live
execution; MLflow is the model-lifecycle candidate; Qdrant is the retrieval candidate;
OpenTelemetry is the observability baseline; OpenBB is the research/data candidate.

The current architecture keeps these as optional adapters rather than hard dependencies.
This is deliberate for the native Windows development target and the project's limited
local resources. Integration work should add one adapter at a time with deterministic
contract tests and a measured resource/performance baseline.

The current adapter wave provides real infrastructure implementations for all five
selected candidates without making any of them mandatory in the base installation.
NautilusTrader uses its low-level BacktestEngine boundary; MLflow uses model
registration/alias resolution; Qdrant uses its Query API; OpenTelemetry caches metric
instruments; OpenBB uses the ODP Python interface for research news retrieval.
