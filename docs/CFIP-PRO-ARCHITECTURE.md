# CFIP-PRO Architecture Foundation

## Boundary model

CFIP-PRO uses a modular monolith with explicit boundaries. Modules are organized by responsibility and later by capability; there is no premature microservice split.

```text
Browser / Terminal
        │
        ▼
Next.js App Router
        │
        │ typed HTTP/event contracts
        ▼
FastAPI transport
        │
        ▼
Application use cases
        │
        ▼
Domain contracts / policies
        │
        ├───────────────┬─────────────────┐
        ▼               ▼                 ▼
PostgreSQL         NATS JetStream      Redis
(system of record) (durable events)   (cache/coord.)
```

## Backend package rules

- `api`: HTTP transport only. It maps requests to application use cases and maps results to response contracts.
- `application`: orchestration, use cases, transaction coordination, ports, policies.
- `domain`: business concepts, value objects, domain events, pure contracts. No FastAPI, SQLAlchemy, Redis or NATS imports.
- `infrastructure`: implementations for persistence, messaging, cache, external providers and operational adapters.
- `worker`: process entry points and consumer lifecycle. Business logic belongs in application/domain modules, not worker loops.

Dependencies flow inward. Infrastructure can depend on application/domain; domain never depends on infrastructure.

## Frontend package rules

- `app`: routing, layouts and route-level composition.
- `components`: reusable visual primitives and terminal shell components.
- `features`: capability-owned UI, hooks and state; a feature may not reach into another feature's internals.
- `lib`: transport clients, validation, configuration and cross-cutting utilities.

The terminal is chart-first. Core interaction uses a full-height chart, tool rail, inspector/drawers and compact status bars rather than dashboard-style scrolling pages.

## Data contracts

HTTP and event contracts are versioned independently of implementation modules. Provider-specific payloads are normalized before entering domain/application flows.

The first canonical event artifact is `EventEnvelope`. It establishes event id, event name/version, timestamp, producer, correlation id and payload. Capability-specific event schemas will extend this boundary rather than bypass it.

## Persistence

PostgreSQL is the transactional source of truth. SQLAlchemy async access is isolated in infrastructure. Alembic owns schema evolution. No application startup path is allowed to silently mutate schema.

ClickHouse is not part of the foundation runtime until workload evidence justifies analytical separation.

## Messaging

NATS JetStream is the intended durable event backbone. The initial adapter is intentionally thin. Streams, consumers, retry policy, idempotency and replay semantics are introduced with the first real event-driven vertical slice.

## Cache

Redis is available as an infrastructure capability for cache, rate limiting, ephemeral coordination and short-lived state. It is never the source of truth for durable business state.

## Frontend charting

TradingView Lightweight Charts 5.2.1 is the initial chart engine. The chart component owns lifecycle, resize observation and disposal. Market data will be supplied through a feature contract rather than embedded demo data once the market-data vertical slice begins.

## Dependency policy

Core production paths use stable releases only. As of 2026-09-17 the foundation records the selected stable versions/ranges in `pyproject.toml` and `apps/web/package.json`. Prerelease SQLAlchemy 2.1 is deliberately excluded because its current 2.1 release is still a release candidate; SQLAlchemy 2.0.54 is the current stable line.

Dependency updates require a compatibility check, tests, and a progress-record entry rather than blind upgrades.
