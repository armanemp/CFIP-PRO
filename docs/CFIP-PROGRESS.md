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
- Added React 19.3, TypeScript 6.0.3, Tailwind CSS 4.3, Zod, TanStack Query and Lightweight Charts dependencies.
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

**Stage:** Chart terminal + single native application entrypoint

**Foundation:** implemented and locally verified

**Market data vertical slice:** implemented and locally verified

**PostgreSQL schema/migration:** implemented and verified with real PostgreSQL

**Transactional outbox:** implemented and locally verified

**NATS JetStream publisher/consumer boundaries:** implemented; live native runtime verification remains pending

**Market API:** implemented

**Frontend market-data API client:** implemented with Zod validation

**Chart module:** expanded into an interactive terminal chart module with multiple chart types, timeframes, volume, technical overlays, RSI, drawing tools, FVG/Order Block/structure detection foundations, crosshair, zoom/pan and fit controls

**Single entrypoint:** implemented. Native launcher builds the static Next.js web application when needed and FastAPI serves the web UI plus `/api/*` from port `8000` on the same origin.

**Production readiness:** not claimed

## 2026-09-17 — Native verification after market vertical slice

- Real PostgreSQL integration test passed: `1 passed in 0.82s`.
- Full local pytest passed: `7 passed, 2 warnings in 1.08s`.
- Ruff passed: `All checks passed!`.
- Mypy passed: `Success: no issues found in 26 source files`.
- The temporary `CFIP_TEST_DATABASE_URL` environment variable was removed after verification.
- No database reset, migration rerun, dependency installation or Docker/WSL action was required for this verification.

## 2026-09-17 — Full chart terminal module

- Expanded `apps/web/src/components/market-chart.tsx` from a basic observation-derived chart into the first full interactive chart terminal module.
- Added selectable 1m/5m/15m/1H/4H/1D timeframes with observation-to-OHLC aggregation.
- Added Candlestick, OHLC Bars, Line and Area rendering modes.
- Added volume histogram, SMA20, EMA20, EMA50, Bollinger Bands and VWAP overlays.
- Added an RSI oscillator panel.
- Added crosshair, wheel zoom, drag pan, axis scaling and fit-to-content controls.
- Added client-side drawing tools for cursor, horizontal line, vertical line and trendline plus clear-drawings control.
- Added FVG, Order Block and market-structure detection foundations as chart overlays/annotations. These are detection foundations only; canonical lifecycle semantics will be introduced in the dedicated market-intelligence vertical slices.
- Kept the chart data boundary API-backed; no synthetic market dataset was introduced.
- No new chart dependency was installed because Lightweight Charts 5.2.1 was already the selected stable chart engine.

## 2026-09-17 — Single port 8000 native application entrypoint

- Corrected the execution model so the user does not need a second terminal for the frontend.
- Configured Next.js for a static export under `apps/web/out`.
- Changed the frontend API client default from a cross-origin hardcoded API URL to same-origin `/api`, while retaining `NEXT_PUBLIC_API_BASE_URL` as an explicit override.
- Updated FastAPI to serve the generated web application from the same process and origin as the API on port `8000`.
- Added `scripts/run_cfip.py` as the single native Windows launcher. It uses the current project Python executable, builds the web application only when `apps/web/out/index.html` is missing, and then starts Uvicorn on port `8000`.
- The launcher does not install dependencies, reset PostgreSQL, require Docker/WSL, or create a second frontend process.
- Updated README to make port `8000` the canonical CFIP-PRO application entrypoint.

## Verification state

**Python 3.14.7:** PASS

**Backend import/boot:** PASS

**Mypy:** PASS — no issues found in 26 source files

**Ruff:** PASS — all checks passed

**Full pytest:** PASS — 7 passed, 2 warnings

**Real PostgreSQL integration:** PASS — 1 passed

**PostgreSQL schema:** PASS — Alembic `0001_market_data`

**Chart implementation:** COMMITTED — native browser execution pending after pull

**Single port 8000 entrypoint:** COMMITTED — native execution pending after pull

**Latest chart CI:** queued at the time of implementation; must be checked after GitHub Actions completes

**Production readiness:** not claimed

## Next execution order

1. Pull the latest `main` state locally.
2. Run the single native launcher: `\.venv\Scripts\python.exe scripts\run_cfip.py`.
3. Open `http://127.0.0.1:8000`; do not start a separate frontend terminal.
4. Verify the static web build and API are reachable from the same origin.
5. Run the backend test/lint/typecheck gates after the pull.
6. Run frontend lint/typecheck/build if the launcher had to create a fresh web build.
7. Check fresh GitHub Actions status before declaring the chart/entrypoint commit green.
8. Verify live NATS JetStream runtime only when a native NATS instance is intentionally available; do not introduce Docker/WSL into the workflow.
9. Continue chart work with canonical historical OHLC semantics, realtime streaming, MTF synchronization, persistent drawings, indicator lifecycle and replay/backtest parity rather than adding disconnected demo features.
10. Continue capability-by-capability OSS evaluation before adding specialized libraries.
11. Keep this file updated after every meaningful step.

## Operational rule for the next session
Do not generate another parallel architecture, progress file, duplicate prompt, or duplicate workflow. Continue from this repository state and append to this file.

## Environment rule — mandatory
- All Python execution for CFIP-PRO development is performed inside the project `.venv`.
- On Windows the canonical executable is `C:\Users\armanemp\Desktop\CFIP-PRO\.venv\Scripts\python.exe`.
- Prefer explicit `\.venv\Scripts\python.exe -m ...` commands; if the environment is activated, verify `sys.executable` before Python checks.
- Docker and WSL are not part of the current native Windows development workflow.
- The canonical user-facing native application entrypoint is port `8000`; the frontend must not require a separate manual development server for the normal CFIP-PRO run.
