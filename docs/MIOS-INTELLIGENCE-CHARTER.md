# MIOS Intelligence Charter

MIOS is the active presentation name for the CFIP-PRO intelligence system.

## Scope

MIOS is intended to become a complete market-intelligence system spanning:

- market data ingestion, normalization, quality and freshness;
- technical and structural analysis;
- evidence-backed research and retrieval;
- model/agent/tool orchestration;
- signal generation with explicit uncertainty;
- risk-aware execution planning;
- deterministic replay/backtesting;
- outcome attribution, calibration and drift detection;
- governed training and model lifecycle;
- self-healing diagnostics and fail-closed repair gates;
- self-development proposals, validation, approval and rollback;
- security, provenance, observability and auditability.

## Architectural rule

The product name is presentation metadata. Domain and application contracts remain brand-neutral under `intelligence.core`. Adapters may use OSS implementations, but vendor-specific objects must not cross the CFIP domain boundary.

## Learning loop

```text
Market/Data
    -> Quality + Provenance
    -> Canonical Analysis
    -> MIOS Evidence/Retrieval
    -> Signal + Uncertainty
    -> Risk Gate
    -> Replay/Execution Simulation
    -> Outcome
    -> Calibration + Drift
    -> Dataset/Feature Lineage
    -> Model Registry
    -> Governed Promotion or Rollback
    -> Self-Healing / Self-Development Evidence
    -> next learning cycle
```

## Safety invariants

Self-healing and self-development are governed capabilities, not unrestricted mutation:

1. protected health invariants must be observable and healthy;
2. proposals require explicit evidence and validation plans;
3. production-affecting promotion requires an authorization boundary;
4. rollback information is mandatory for material changes;
5. provenance and artifact identity must remain auditable;
6. failed validation must fail closed;
7. OSS adoption requires deterministic tests and runtime/resource evidence.

MIOS therefore learns continuously from outcomes and evidence while keeping deployment authority separate from proposal generation.
