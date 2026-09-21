# CFIP-PRO OSS Integration Roadmap

Updated: 2026-09-21

CFIP-PRO uses an OSS-first strategy: CFIP owns contracts, governance, product UX and domain-specific orchestration; mature open-source projects provide implementation engines behind adapters. No library is accepted solely because it is popular. Every integration must pass compatibility, licensing, resource, security, deterministic-behavior and maintenance gates.

## Target capability map

| Capability | OSS candidate | CFIP role | Integration status |
|---|---|---|---|
| Financial data / research | OpenBB | Provider-neutral data/research adapter | Candidate; wire after provider contract audit |
| Market/exchange connectivity | ccxt | Crypto/venue adapter where applicable | Dependency present; adapter health gate required |
| Trading/backtest/execution engine | NautilusTrader | Deterministic simulation/live execution adapter | Candidate; isolate optional dependency |
| Quant research / ML | Qlib | Research/training adapter | Candidate; benchmark before runtime dependency |
| Technical indicators | TA-Lib | Indicator adapter | Dependency present; migrate native calculations behind adapter contract |
| Columnar analytics | Polars / Arrow | Dataframe/feature computation | Polars present; Arrow boundary to be evaluated |
| Experiment lifecycle | MLflow | Experiment/model registry adapter | Planned |
| Feature store | Feast | Online/offline feature contract adapter | Planned |
| Drift / monitoring | Evidently | Data/model drift evaluation adapter | Planned |
| Observability | OpenTelemetry | Traces/metrics/log correlation | Planned |
| LLM observability | Langfuse / Phoenix | Intelligence tracing/evaluation | Planned; choose one primary after benchmark |
| Agent orchestration | PydanticAI / LangGraph / Haystack | MIOS adapter layer | PydanticAI present; benchmark alternatives before adding overlap |
| Vector retrieval | pgvector / Qdrant | Evidence retrieval adapter | Planned; PostgreSQL-first default |
| Search | OpenSearch | Research/evidence search adapter | Planned only if PostgreSQL search is insufficient |
| Web acquisition | trafilatura / Scrapy / Playwright / Firecrawl | Research acquisition adapters | Planned; use least-powerful sufficient adapter |
| Graph | Apache AGE / Kuzu / Neo4j | Relationship/evidence graph adapter | Deferred until benchmark demonstrates need |
| Authorization | OpenFGA / OPA / Keycloak | Policy/IAM adapters | Planned; fail-closed governance remains CFIP-owned |

## Integration rule

OSS components never bypass CFIP contracts. The dependency direction is:

`OSS implementation -> CFIP adapter -> CFIP domain contract -> application orchestration -> API/UI`

Not:

`UI -> OSS library` or `domain -> vendor-specific API`.

## Acceptance gates

1. Python 3.14 compatibility and Windows development viability where applicable.
2. License compatibility documented before adoption.
3. Security/advisory review and dependency pinning.
4. Resource budget appropriate for the user's lightweight development environment.
5. Deterministic behavior for backtest/replay paths.
6. Typed input/output mapping with provenance and timestamps.
7. Failure isolation: optional integrations must not prevent CFIP core startup.
8. Health/readiness reporting must distinguish installed, configured, reachable and operational.
9. No duplicate engine is added when an existing adapter can provide the capability.
10. Benchmark evidence is recorded before replacing a stable native path.

## Immediate sequence

- TA-Lib adapter: move terminal indicator calculation behind the existing registry contract and add parity tests.
- OpenBB adapter: implement research/market-data normalization without forcing all providers to be installed.
- NautilusTrader adapter: establish backtest/replay/execution boundary without coupling the terminal to the engine.
- MLflow + Evidently: connect experiment/outcome/drift lifecycle to MIOS learning contracts.
- OpenTelemetry + intelligence tracing: make every analysis and self-development proposal traceable.
- Retrieval stack: PostgreSQL/pgvector first; add Qdrant/OpenSearch only after measured need.
- Research acquisition: trafilatura first, Playwright only for pages requiring browser execution.
