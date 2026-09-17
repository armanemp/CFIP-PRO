# CFIP-PRO Canonical Progress Record

**Canonical rule:** this file is the single project progress/change ledger. Every meaningful implementation, dependency decision, verification result, blocker and next step is appended here. Do not create competing progress ledgers.

## 2026-09-17 — Foundation implementation started

### Repository state before this work
- `main` contained only the Master Prompt and Development Workflow.
- No executable backend, frontend, dependency manifest, tests, database migration foundation, or runtime shell existed in the repository.
- The project contract was already defined as a clean greenfield Python-first implementation.

### Implemented now
- Created Python 3.14 project metadata in `pyproject.toml` using `uv`-compatible dependency groups.
- Added stable backend foundation: FastAPI, Pydantic 2, Pydantic Settings, SQLAlchemy 2, Alembic, async PostgreSQL driver boundary, NATS client boundary, Redis client dependency and Uvicorn.
- Established backend package boundaries under `apps/api/src/cfip/`: `api`, `application`, `domain`, `infrastructure`, and `worker`.
- Added explicit settings/configuration module and safe `.env.example`.
- Added canonical `EventEnvelope` contract.
- Added PostgreSQL async engine/session boundary.
- Added NATS adapter boundary.
- Added minimal worker process entry point.
- Added FastAPI root and `/api/health` endpoint.
- Added a real backend health test.
- Created Next.js 16 App Router frontend foundation under `apps/web`.
- Added React 19.3, TypeScript 7, Tailwind CSS 4.3, Zod, TanStack Query and Lightweight Charts dependencies.
- Added a chart-first terminal shell with tool rail, chart area, inspector and status bar.
- Added a lifecycle-safe Lightweight Charts component with resize observation and cleanup.
- Added frontend API client boundary with Zod validation.
- Added TypeScript/Next/Tailwind configuration and ESLint flat config.
- Added repository `.gitignore` and README.
- Added `docs/CFIP-PRO-ARCHITECTURE.md` as the structural architecture reference.
- Added `alembic.ini` and migration environment without creating a fake business schema.
- Added optional infrastructure compose configuration for PostgreSQL, NATS JetStream and Redis. It is not part of the normal native Windows development loop.
- Added a native Windows verification script at `scripts/dev.ps1`.
- Added GitHub Actions CI for backend lint/test and frontend typecheck/lint/build.
- Kept `docs/CFIP-MASTER-PROMPT.md` and `docs/CFIP-DEVELOPMENT-WORKFLOW.md` as the governing project contract; this implementation is consistent with their current rules, so no destructive rewrite was necessary.

### Dependency verification basis
- FastAPI 0.141.1 is the latest release identified in the official release notes during this work.
- Pydantic 2.13.4 is the latest stable release identified during this work.
- Pydantic Settings 2.15.0 is the latest stable release identified during this work.
- SQLAlchemy 2.0.54 is the current stable release; SQLAlchemy 2.1.0rc2 is still prerelease and therefore excluded from the production foundation.
- Alembic 1.20.0 was released 2026-09-11.
- Redis Python client 8.1.0 is the latest stable release identified during this work.
- Ruff 0.16.8 is the latest release identified during this work.
- pytest 9.1.1 is the latest release identified during this work.
- Next.js 16.3.3 is the active-LTS patch level identified from the August 2026 security release.
- React 19.3 is stable as of 2026-09-09.
- TypeScript 7.0.2 was initially selected, then changed to stable TypeScript 6.0.3 because the current eslint-config-next/typescript-eslint toolchain supports TypeScript <6.1.0.
- Tailwind CSS 4.3 is the current major/minor stable line identified from the official Tailwind release notes.
- TanStack Query 5.103.1 and Lightweight Charts 5.2.1 were current stable npm releases identified during this work.

### Important design decision
The foundation is intentionally executable but not falsely feature-complete. Market data, FVG, Order Blocks, signals, intelligence, auth, payments, journal, backtesting, replay, governance and other product capabilities are not represented as completed merely by placeholder files. They will enter through vertical slices with contracts, implementation, tests and verification.

## Current stage

**Stage:** First market-data vertical slice / implementation

**Contract:** established by Master Prompt + Development Workflow

**Foundation:** implemented and locally verified

**Market domain contract:** implemented

**PostgreSQL schema/migration:** implemented; runtime migration verification pending local PostgreSQL

**Transactional outbox:** implemented

**NATS JetStream publisher boundary:** implemented with message-id deduplication

**JetStream consumer boundary:** implemented with explicit acknowledgements and bounded redelivery

**Market API:** implemented

**Frontend market-data API client:** implemented with Zod validation

**Chart integration:** switched from synthetic candles to real observation-derived line data; no synthetic market values are generated

**Production readiness:** not claimed

**GitHub CI baseline:** PASS for commit `2a42e0d`; new vertical-slice CI is currently being corrected after lint findings

## 2026-09-17 — Native verification and CI baseline

- Native Windows backend verification passed using the project `.venv`: pytest passed, Ruff passed, and mypy reported no issues across the backend source.
- Native Windows frontend verification passed after the TypeScript compatibility correction: typecheck passed, lint passed with one non-blocking existing PostCSS anonymous-default-export warning, and Next build completed successfully.
- Next.js generated local changes to `next-env.d.ts` and `tsconfig.json`; those generated changes were restored and were not committed.
- `npm install` created `apps/web/package-lock.json`; it was intentionally removed because the repository has not established a lockfile policy and CI currently uses `npm install`.
- GitHub Actions run `35260452595` for commit `2a42e0d` completed successfully.
- No `npm audit fix --force` action was taken despite npm reporting two vulnerabilities; dependency changes require explicit compatibility/security analysis rather than forced remediation.
- Docker and WSL remain outside the current native Windows development workflow.

## 2026-09-17 — First market-data vertical slice implementation

- Added framework-independent contracts for instruments, normalized market observations and observation queries in `apps/api/src/cfip/domain/market.py`.
- Added PostgreSQL persistence models for `instruments`, `market_observations`, and `outbox_events`.
- Added the first Alembic migration `0001_market_data` with UUID identifiers, timezone-aware timestamps, high-precision numeric market values, instrument/venue uniqueness, observation indexing, source-event uniqueness boundary and transactional outbox state.
- Updated Alembic to use the async PostgreSQL driver and the SQLAlchemy metadata for online migrations.
- Added repository and application-service boundaries. Observation ingestion writes the observation and its `market.observation.recorded` outbox event in the same database transaction.
- Added FastAPI routes for instrument creation/listing, observation ingestion and observation queries by symbol/venue/time window.
- Added a JetStream stream boundary for `market.>` subjects and message-id based publish deduplication.
- Added a durable pull consumer boundary with explicit acknowledgement, negative acknowledgement on handler failure and `max_deliver=5` redelivery protection.
- Added a transactional outbox relay worker that claims pending rows with PostgreSQL `FOR UPDATE SKIP LOCKED`, publishes them using the outbox UUID as the NATS message id, records attempts/errors and marks successful publication.
- Replaced the frontend synthetic candle dataset with API-backed normalized market observations. The chart now renders a line series from real `last`/`bid`/`ask` observations and never fabricates OHLC values from sparse ticks.
- Added Zod validation for the market observation response shape.
- Added unit coverage for market contracts and event serialization.
- Corrected README frontend version documentation from TypeScript 7 to TypeScript 6.0.3.
- NATS JetStream consumer semantics were checked against current nats.py documentation before adding the consumer boundary; the repository remains on its existing `nats-py` dependency line and no new specialized dependency was introduced.

## 2026-09-17 — Fresh CI lint correction

- CI run `35261839952` reached the new vertical-slice commit and failed only in the backend Ruff step before pytest could run.
- The exact Ruff findings were two E501 lines: `apps/api/src/cfip/infrastructure/messaging/consumer.py` and `tests/unit/test_market_domain.py`.
- Both lines were split without changing behavior.
- During the same CI sequence, the market route dependency signature was corrected to use an explicit `Annotated[..., Depends(...)]` dependency boundary and then adjusted so the dependency parameter remains valid after default-valued query parameters.
- The latest `main` ref now contains those corrections; a fresh CI run is required before treating the vertical slice as green.

## 2026-09-17 — Clean native PostgreSQL runtime verification

- The local PostgreSQL 18.6 service was verified running natively on Windows at `127.0.0.1:5432`.
- The development `cfip` database was explicitly deleted and recreated as a clean database at the user's request. This reset is now complete; do not reset the database again unless the user explicitly requests it.
- Database ownership and the `public` schema ownership were aligned with the `cfip` role so Alembic can manage the schema without elevated privileges.
- Alembic `upgrade head` completed successfully and applied migration `0001_market_data`.
- Direct PostgreSQL verification confirmed `alembic_version=0001_market_data` and the expected `instruments`, `market_observations`, and `outbox_events` tables.
- Column verification confirmed UUID identifiers, timezone-aware timestamps, high-precision numeric market values, source-event fields and outbox publication state.
- The repository contains only `.env.example`; there is intentionally no committed `.env` or credential-bearing configuration file.

## 2026-09-17 — Real PostgreSQL integration proof added

- Added `tests/integration/test_market_postgres.py` as the first real database integration test.
- The test uses `CFIP_TEST_DATABASE_URL` supplied by the local environment and never embeds a database password in source code.
- The test creates a unique instrument, ingests a real normalized observation through `MarketService`, then verifies the persisted observation and the corresponding `market.observation.recorded` outbox event in PostgreSQL.
- Cleanup removes the test outbox event, observation and instrument after successful verification, preventing test records from accumulating in the development database.
- Registered the `integration` pytest marker in `pyproject.toml`.
- The integration test is intentionally explicit rather than silently falling back to SQLite or mocks; when `CFIP_TEST_DATABASE_URL` is absent, pytest reports the integration test as skipped with a clear reason.

## Verification state after the current native work

**Python 3.14.7:** PASS

**Backend import/boot:** PASS

**Mypy:** PASS — no issues found in 26 source files

**Ruff:** PASS — all checks passed

**Unit test suite:** PASS — 6 passed, 2 deprecation warnings

**PostgreSQL service:** PASS — PostgreSQL 18.6 running natively

**Alembic migration:** PASS — `0001_market_data`

**PostgreSQL schema verification:** PASS

**Real PostgreSQL integration test:** added; local execution requires `CFIP_TEST_DATABASE_URL`

**NATS JetStream runtime integration:** not yet executed against a live local NATS instance

**Frontend verification after the latest backend commits:** not yet re-run after the latest repository changes

**Fresh GitHub CI after the latest integration commits:** not yet verified

**Production readiness:** not claimed

## Next execution order

1. Pull the latest `main` state locally.
2. Set `CFIP_TEST_DATABASE_URL` only in the local PowerShell session using the user's existing PostgreSQL credentials; never commit it.
3. Run the real PostgreSQL integration test and confirm it passes against the clean `cfip` database.
4. Run the complete backend verification again: pytest, Ruff and mypy.
5. Verify the outbox relay and JetStream stream/consumer against a reachable native NATS JetStream instance.
6. Re-run the frontend typecheck/lint/build after the current repository state is pulled.
7. Check fresh GitHub Actions status for the resulting commits before declaring the vertical slice green.
8. Exercise the API with an explicitly submitted real observation, then confirm the frontend renders persisted observation data rather than synthetic values.
9. Only after this slice is green, add provider adapters and candle aggregation semantics; do not infer OHLC candles from arbitrary sparse observations.
10. Continue capability-by-capability OSS evaluation before adding specialized libraries.
11. Keep this file updated after every meaningful step.

## Operational rule for the next session
Do not generate another parallel architecture, progress file, duplicate prompt, or duplicate workflow. Continue from this repository state and append to this file.

## Environment rule — mandatory
- All Python execution for CFIP-PRO development is performed inside the project `.venv`.
- On Windows the canonical executable is `C:\Users\armanemp\Desktop\CFIP-PRO\.venv\Scripts\python.exe`.
- Prefer explicit `\.venv\Scripts\python.exe -m ...` commands; if the environment is activated, verify `sys.executable` before Python checks.
- Docker and WSL are not part of the current native Windows development workflow.
