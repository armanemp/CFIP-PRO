# CFIP Terminal Development Rules

## UI
The terminal remains chart-first, minimal, dense and professional. New controls must be contextual and avoid dashboard-style scrolling.

## Modularity
Indicators, analysis, intelligence, market data, risk, alerts, notifications, workspaces, drawings and execution must remain replaceable domain modules. Presentation components consume contracts; they do not own domain algorithms.

## Hardcode policy
Do not hardcode user-configurable values in UI components. Symbol lists, timeframes, indicator definitions, risk limits, provider choices, locales and feature flags belong to registries/configuration contracts. Secrets never belong in source.

## Intelligence
Aevrix may research, diagnose, evaluate and propose. Code/model/data changes must carry evidence, provenance, validation, rollback information and an explicit promotion boundary.

## Git boundary
Repository inspection may be automated. Mutations are represented as governed proposals and require authorization. Protected CI/security/environment files are not mutable through the generic AI change boundary. Shell execution is outside the domain contract.

## OSS
OSS is integrated through adapters/contracts after license, maintenance, API-fit, security, performance and dependency review. The platform must remain portable if an OSS component is replaced.

## Verification
Never claim CI green without an actual workflow result. Each implementation batch should be followed by static inspection and then repository CI/local verification when available.
