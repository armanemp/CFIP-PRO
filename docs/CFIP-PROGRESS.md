## 2026-09-21 — MIOS runtime contracts, governed self-development and security boundary

- Added canonical vendor-neutral Agent/Tool/Model/Memory/Research runtime contracts with bounded execution context, evidence/provenance, confidence, usage and trace metadata.
- Added a lazy PydanticAI agent adapter that normalizes provider output into the CFIP contract without importing vendor types into the domain.
- Added governed self-development state transitions: observed → diagnosed → proposed → validated → approved → applied, with explicit approval and rollback invariants.
- Added unit tests proving that promotion cannot bypass approval, applied changes require a revision, and rollback requires revision evidence.
- Registered the intelligence runtime and HTTP security boundary in the capability registry; the PydanticAI implementation is classified as an adapter rather than a core dependency.
- Hardened the HTTP boundary with bounded request correlation IDs, configurable trusted hosts, production HSTS, and explicit rejection of wildcard CORS configuration.
- Updated the progress chart to reflect the new governed self-development and security capabilities.
- Current verification limitation: GitHub reports no CI workflow/status for the latest commit, so CI green is not claimed.

## 2026-09-21 — governed health evaluation and invariant API

- Added API-level component health evaluation backed by the existing deterministic circuit-breaker policy.
- Added protected health-invariant evaluation as an explicit API boundary; missing safety evidence remains blocking rather than inferred healthy.
- Added regression coverage for repeated component failure, fail-closed invariant evaluation, and the existing browser security headers.
- This creates the runtime-facing health input required by the self-healing execution gate without granting the health endpoint mutation authority.

## 2026-09-21 — broker-aware risk planning API vertical slice

- Exposed the existing deterministic RiskService through POST /api/risk/plan, with explicit account context, instrument/broker constraints and quote-to-account conversion.
- The API returns the canonical RiskTargetPlan; missing context, invalid conversion, broker minimums and margin constraints remain fail-closed.
- Added endpoint regression tests for unavailable-without-context and a complete EUR/USD-style broker-aware sizing plan.
- Removed the superseded duplicate domain/risk_contracts.py contract so the canonical domain/risk.py boundary is the only risk contract.
- Next completion focus remains connecting real broker/instrument registries to this API and wiring Entry/SL/TP/risk output into the terminal without inventing broker metadata.

## 2026-09-21 — OSS adapter fabric and evidence-backed health boundary

- Added optional infrastructure adapters for MLflow Model Registry, Qdrant retrieval and OpenTelemetry observability. They use lazy vendor imports so the core native development environment remains lightweight.
- Qdrant adapter now performs real vector query/upsert operations when an embedding provider is explicitly injected; it fails closed if embeddings are not configured.
- Added typed health signals with freshness checks and explicit evidence IDs. Self-healing can now consume evidence-backed observations rather than synthetic health claims.
- Added adapter contract tests and health freshness/duplicate-signal tests.
- OSS selection remains capability-specific: NautilusTrader for deterministic trading/backtest/live semantics, MLflow for model lifecycle and lineage, Qdrant for hybrid/vector retrieval, and OpenTelemetry for vendor-neutral traces/metrics.
- Heavy OSS packages remain optional rather than forced into the native Windows environment; each adapter must still pass compatibility, semantic, resource, security and rollback gates before becoming a core dependency.

## 2026-09-21 — governed learning, dataset leakage and artifact integrity hardening

- Added a deterministic temporal dataset-split contract with explicit as_of boundaries and fail-closed overlap/future-example checks.
- Added real SHA-256 artifact digest computation and constant-time verification using the Python standard library.
- Added a governed learning-promotion policy requiring a healthy platform, independent evidence, minimum outcome volume, minimum confidence and zero contradictions by default.
- Added focused unit coverage for temporal leakage, artifact tampering and learning-promotion failure modes.
- Current OSS strategy remains adapter-first: NautilusTrader for trading/backtest, MLflow for model lifecycle, Qdrant/pgvector for retrieval, OpenTelemetry for observability and OpenBB for research/data.
- GitHub connector currently reports repository push permission. The newest commits do not yet expose a completed CI status through the connector, so CI green is not claimed.

## 2026-09-20 — deep terminal hardening and intelligence naming cleanup