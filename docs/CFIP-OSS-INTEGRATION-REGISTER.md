# CFIP-PRO — OSS Integration Register

Status: active engineering policy
Date: 2026-09-20

## Selection rule

CFIP-PRO uses mature OSS where it removes substantial bespoke implementation without compromising the platform's commercial model, Python 3.14 baseline, causal market semantics, or architectural independence.

OSS is an implementation component, not the CFIP decision authority. CFIP owns the canonical analysis contract, evidence model, confluence gates, governance and product semantics.

## Integrated

### TA-Lib

- Package: TA-Lib==0.8.0
- Role: classical OHLCV indicators in the backend analysis engine.
- Used for: EMA, RSI, ATR, ADX/+DI/-DI, MACD and Bollinger Bands.
- Reason: removes duplicated numerical indicator code while retaining deterministic, testable outputs.
- Python baseline: compatible with the project's Python 3.14 target.
- CFIP-specific logic remains outside TA-Lib: FVG lifecycle, liquidity, structure, displacement, premium/discount and confluence.

### pyvsmc

- Repository: Khaymat/pyvsmc
- Package: pyvsmc==0.3.7
- License: MIT
- Role: SMC/market-structure primitives.
- Integrated now: FVG candidate detection in the canonical backend analysis engine.
- CFIP retains lifecycle state, causal invalidation and confluence semantics around the OSS detector.

### Polars

- Repository: pola-rs/polars
- Package: polars==1.44.2
- License: MIT
- Role: high-throughput tabular/research/data-engineering substrate.

### PydanticAI

- Repository: pydantic/pydantic-ai
- Package: pydantic-ai==2.44.0
- License: MIT
- Role: governed typed agent runtime.
- Deterministic analysis/risk/release boundaries remain authoritative.

### CCXT

- Repository: ccxt/ccxt
- Package: ccxt==4.5.78
- License: MIT
- Role: unified crypto exchange market-data/trading connectivity boundary.

## Optional adapters implemented — verification gated

### NautilusTrader

- Role: deterministic backtest/replay/trading-engine boundary.
- Adapter: `apps/api/src/cfip/infrastructure/oss/nautilus_trader_engine.py`
- Integration shape: CFIP passes vendor-native instruments, venues, data batches and strategies only at the infrastructure boundary; results are reduced to CFIP-neutral dictionaries.
- Dependency remains optional because NautilusTrader is not required for the lightweight native development environment.
- Gate remaining: runtime integration fixture, deterministic benchmark, resource baseline and live reconciliation adapter.

### MLflow

- Role: model registry, versioning, aliases, metadata and lifecycle provenance.
- Adapter: `apps/api/src/cfip/infrastructure/oss/mlflow_registry.py`
- Integration shape: registration and alias resolution are asynchronous wrappers around MLflow; domain/application code sees only version/source strings.
- Gate remaining: database-backed registry integration, lineage metadata verification and rollback exercise.

### Qdrant

- Role: retrieval index for research memory.
- Adapter: `apps/api/src/cfip/infrastructure/oss/qdrant_retrieval.py`
- Integration shape: injected embedding provider, validated vectors, bounded result size, payload normalization and explicit client lifecycle.
- Gate remaining: collection schema contract, dense+sparse hybrid retrieval benchmark and local low-memory benchmark.

### OpenTelemetry

- Role: traces and metrics baseline.
- Adapter: `apps/api/src/cfip/infrastructure/oss/opentelemetry_observability.py`
- Integration shape: CFIP owns the semantic event names; OTel owns transport/export.
- Gate remaining: SDK/exporter configuration, trace propagation through analysis/self-healing and metric cardinality review.

### OpenBB

- Role: external financial research/news retrieval.
- Adapter: `apps/api/src/cfip/infrastructure/oss/openbb_research.py`
- Integration shape: controlled/lazy import and serialization into plain dictionaries.
- Gate remaining: provider provenance normalization, source freshness policy, memory benchmark and research evidence tests.

## Evaluated but deliberately not embedded

### VectorBT

VectorBT remains a research option but is not a product-core dependency until licensing and distribution constraints are explicitly cleared.

### Backtesting.py

Backtesting.py remains a lightweight research option but its AGPL-3.0 license keeps it outside the distributable product runtime.

### Backtrader

Backtrader remains outside product core because its GPLv3+ licensing and older architecture do not fit the current direction.

## Engineering consequence

The product runtime remains CFIP-owned at the contract layer. OSS adapters are replaceable
implementation modules with deterministic gates. A capability is not marked production-ready
until its semantic, performance, resource, security, rollback and provenance evidence exists.

## Current dependency policy

- Prefer permissive licenses suitable for a commercial product.
- Prefer current Python 3.14 support.
- Prefer stable releases over development branches.
- Do not add an OSS package solely because it has more features.
- Do not duplicate a mature numerical implementation inside CFIP.
- Keep CFIP-specific market-structure semantics explicit and auditable.
- Keep heavyweight research/observability engines optional and lazily imported for native development.
