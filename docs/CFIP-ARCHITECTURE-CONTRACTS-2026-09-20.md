# CFIP-PRO — Platform Contract Boundaries — 2026-09-20

This document records the current direction for replacing static implementation coupling with explicit contracts and adapters.

## Terminal

The terminal is split conceptually into workspace state, market-data acquisition, chart rendering, indicator runtime, analysis overlays, drawing history/controller, replay and command/keyboard interaction. The V3 surface remains the composition layer; new behavior should enter through these boundaries rather than adding another monolithic implementation.

## Indicators

`indicator_contracts.py` is the backend-neutral plugin boundary. OSS engines and native implementations are adapters. Indicator IDs, parameters, versions and normalized output cross the platform boundary; provider-specific SDK types do not.

## Analysis

`analysis_contracts.py` defines normalized evidence and unified aggregation. The aggregation boundary can consume indicator, structure, order-flow, market-data, macro and intelligence evidence without coupling the terminal to one analytics vendor. A final answer is unavailable when minimum confluence is not satisfied.

## Intelligence / Elyrava

Improvement proposals require evidence, a validation plan and rollback conditions. Validation and learning records are separate from proposals. The current in-memory repository is intentionally a reference/local implementation; production persistence must be supplied by a storage adapter. Git and production mutation remain governed boundaries.

## Risk

Risk calculations require explicit account/instrument context and broker constraints. Missing conversion data or invalid constraints produce an unavailable result instead of fabricated sizing. Position sizing must remain independent of broker SDKs.

## Realtime

Stream envelopes provide transport-neutral identity, sequencing and duplicate/out-of-order classification. WebSocket, polling, NATS and replay implementations should normalize into this contract.

## Startup

FastAPI lifespan starts the complete local platform component set together. This means lifecycle initialization is unified; it does not imply that external providers are connected. External readiness must be reported by the configured adapter.

## OSS adoption rule

OSS capabilities are adopted behind CFIP contracts. The project should prefer mature OSS modules where they materially improve capability, but must not accumulate overlapping engines merely because they exist. Selection requires compatibility, license, security, semantic-equivalence and performance review.

## Hardcode rule

Behavioral thresholds, visual tokens, chart layout defaults, provider metadata and operational limits belong in typed configuration/contracts or registries. Presentation code should not recreate palettes or operational defaults locally.

## Governance rule

Elyrava may inspect, research, diagnose, propose and validate. Promotion is governed, high/critical changes require human approval, protected paths remain restricted, and rollback evidence is mandatory. The platform must never represent a catalog entry or local lifecycle initialization as an externally verified connection.
