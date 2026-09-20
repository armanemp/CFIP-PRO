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
| Trading/backtest | NautilusTrader | TradingEngineAdapter | Adapter next |
| ML lifecycle | MLflow | ModelRegistryAdapter | Adapter next |
| Retrieval | Qdrant / pgvector | RetrievalIndexAdapter | Adapter next |
| Observability | OpenTelemetry | ObservabilityAdapter | Adapter next |
| Security scanning | Bandit / pip-audit / Semgrep / OWASP ZAP | SecurityScannerAdapter | Contract added; integration gated |
| Artifact integrity | Sigstore / cryptographic verification tooling | ArtifactVerifier | Contract added; key-management integration gated |
| Research/data | OpenBB | ResearchRetriever | Adapter next |
| Agent orchestration | LangGraph / Haystack | Noverith agent contract | Benchmark |
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

Installation alone never marks a capability complete.

## Selected OSS roles

NautilusTrader is the primary candidate for deterministic research/backtest/live
execution; MLflow is the model-lifecycle candidate; Qdrant is the retrieval candidate;
OpenTelemetry is the observability baseline; OpenBB is the research/data candidate.

The current architecture keeps these as optional adapters rather than hard dependencies.
This is deliberate for the native Windows development target and the project's limited
local resources. Integration work should add one adapter at a time with deterministic
contract tests and a measured resource/performance baseline.
