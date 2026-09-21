# MIOSAIAI OSS Integration Roadmap

Updated: 2026-09-22

MIOSAIAI uses an OSS-first strategy: MIOSAIAI owns contracts, governance, provenance, safety and product UX; mature open-source projects provide implementation engines behind adapters.

| Capability | OSS candidate | MIOSAIAI role | Status |
|---|---|---|---|
| Financial data / research | OpenBB | Provider-neutral research/data adapter | Boundary established; provider-specific coverage remains explicit |
| Market/exchange connectivity | CCXT | Crypto/exchange market-data adapter | Adapter implemented; live credentials/connectivity remain health-gated |
| Trading/backtest/execution | NautilusTrader | Deterministic simulation/execution adapter | Boundary established; typed strategy binding remains |
| Technical indicators | TA-Lib | Indicator adapter | Integrated boundary; parity/coverage expansion remains |
| Columnar analytics | Polars / Arrow | Dataframe/feature computation | Polars present; Arrow adoption benchmarked before expansion |
| Quant research / ML | Qlib | Research/training adapter | Candidate; benchmark before runtime dependency |
| Experiment/model lifecycle | MLflow | Experiment/model registry | Optional profile defined; runtime integration pending |
| Feature store | Feast | Online/offline feature contract | Optional profile defined; integration pending |
| Drift / monitoring | Evidently | Data/model drift evaluation | Optional profile defined; integration pending |
| Observability | OpenTelemetry | Traces/metrics correlation | Optional profile defined; propagation/export pending |
| LLM observability | Langfuse / Phoenix | Intelligence tracing/evaluation | Choose one primary after benchmark |
| Agent orchestration | PydanticAI / LangGraph / Haystack | MIOSAIAI agent adapter layer | PydanticAI present; alternatives remain benchmark candidates |
| Vector retrieval | PostgreSQL/pgvector / Qdrant | Evidence retrieval | PostgreSQL-first; Qdrant optional |
| Search | OpenSearch | Research/evidence search | Optional; add only after measured retrieval need |
| Web acquisition | trafilatura / Scrapy / Playwright / Firecrawl | Research acquisition | trafilatura boundary implemented with SSRF policy |
| Graph | AGE / Kuzu / Neo4j | Relationship/evidence graph | Deferred until benchmarked need |
| Authorization | OpenFGA / OPA / Keycloak | Policy/IAM adapters | Planned; MIOSAIAI governance remains fail-closed |

## Mandatory integration rules

1. OSS dependencies never bypass MIOSAIAI contracts.
2. Optional engines are lazy-loaded and must not prevent core startup.
3. Health must distinguish installed, configured, reachable and operational.
4. Provenance and timestamps are preserved at adapter boundaries.
5. Deterministic replay/backtest behavior is required before an engine controls a production workflow.
6. License, security advisory, resource and maintenance review precede adoption.
7. Do not add duplicate engines without benchmark evidence.
8. External research acquisition must pass the SSRF-safe URL policy and content-rights/provenance checks.
9. Low/medium-risk autonomous changes remain inside the governed change budget; high/critical changes require approval.
