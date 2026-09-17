# CFIP-PRO Canonical Progress Record

**Canonical rule:** this file is the single project progress/change ledger. Every meaningful implementation, dependency decision, verification result, blocker and next step is appended here. Do not create competing progress ledgers.

## 2026-09-17 — Native Windows chart build correction

- The native launcher now correctly resolves Windows `npm.cmd`; the previous `WinError 2` launcher failure is resolved.
- The next build reached Next.js compilation successfully and then exposed a Lightweight Charts 5.2.1 TypeScript error: `scaleMargins` is not a `HistogramSeries` option.
- Corrected `apps/web/src/components/market-chart.tsx` to create the volume histogram with supported series options and apply `scaleMargins` through the histogram series price-scale API: `volume.priceScale().applyOptions(...)`.
- No dependency was added, no cache was disabled, and no database/runtime reset was performed.
- The correction is committed on `main` as `15123bfd895b09bd2495b0fb8a13e49a1e49baaa`.
- Native local verification is still required after pulling this commit.

## 2026-09-17 — Native Windows single-entrypoint correction

- The first implementation of `scripts/run_cfip.py` invoked `npm` directly through Python `subprocess`.
- On Windows, the local failure was `FileNotFoundError: [WinError 2]` because the launcher did not resolve the Windows `npm.cmd` executable shim.
- Corrected the launcher to resolve `npm.cmd` (with `npm` fallback) through `shutil.which()` before invoking the frontend build.
- Added an explicit post-build check for `apps/web/out/index.html` so a successful npm exit cannot silently leave an unusable application entrypoint.
- The launcher still uses the project `.venv` Python executable for the backend and does not install dependencies, reset PostgreSQL, require Docker/WSL, or start a second frontend server.

## Current stage

**Stage:** Chart terminal + single native application entrypoint

**Foundation:** implemented and previously locally verified

**Market data vertical slice:** implemented and previously locally verified

**PostgreSQL schema/migration:** implemented and verified with real PostgreSQL

**Transactional outbox:** implemented and locally verified

**NATS JetStream publisher/consumer boundaries:** implemented; live native runtime verification remains pending

**Market API:** implemented

**Frontend market-data API client:** implemented with Zod validation

**Chart module:** interactive terminal foundation implemented; canonical historical OHLC semantics, realtime streaming, MTF synchronization, persistent drawings and replay/backtest parity remain future vertical slices

**Single entrypoint:** corrected for native Windows; build step resolves `npm.cmd` safely; volume histogram configuration is now compatible with Lightweight Charts 5.2.1; native execution pending after pull

**Production readiness:** not claimed

## Verification state

**Python 3.14.7:** PASS

**Backend import/boot:** PASS

**Mypy:** PASS — no issues found in 26 source files

**Ruff:** PASS — all checks passed

**Full pytest:** PASS — 7 passed, 2 warnings

**Real PostgreSQL integration:** PASS — 1 passed

**PostgreSQL schema:** PASS — Alembic `0001_market_data`

**Chart implementation:** COMMITTED — latest TypeScript compatibility correction committed; native browser execution pending

**Single port 8000 entrypoint:** CORRECTED — native Windows launcher and chart build path corrected; local execution pending

**Latest verification blocker:** local build previously stopped on the unsupported `HistogramSeries` `scaleMargins` option; corrected in commit `15123bfd895b09bd2495b0fb8a13e49a1e49baaa`

**Production readiness:** not claimed

## Next execution order

1. Pull latest `main`.
2. Run `\.venv\Scripts\python.exe scripts\run_cfip.py`.
3. If `apps/web/out/index.html` is missing, the launcher builds it using the Windows-resolved npm command.
4. Open `http://127.0.0.1:8000`; do not start a separate frontend terminal.
5. Verify `/api/health` and the chart terminal from the same origin.
6. Run backend test/lint/typecheck gates after the pull.
7. Run frontend lint/typecheck/build if a fresh web build was created.
8. Check GitHub Actions status for the latest commit before declaring it green.
9. Continue chart hardening with canonical historical OHLC semantics, realtime streaming, MTF synchronization, persistent drawings, indicator lifecycle and replay/backtest parity.
10. Continue capability-by-capability OSS evaluation before adding specialized libraries.
11. Keep this file as the sole progress ledger.

## Operational rules

- CFIP-PRO is a clean greenfield project. The only external project used for capability reference is CForex.
- `cforex-platform` is permanently abandoned and must not be used as an architecture or migration baseline unless explicitly reinstated by the user.
- Native Windows is the current development workflow.
- Docker and WSL are not part of the normal execution loop.
- All Python execution uses the project `.venv`; canonical executable: `C:\Users\armanemp\Desktop\CFIP-PRO\.venv\Scripts\python.exe`.
- Port `8000` is the canonical user-facing application entrypoint. Normal operation must not require a second terminal or manually started frontend server.
- Do not add dependencies or rebuild artifacts unless evidence shows they are required.
- Do not claim production readiness or feature completeness without implementation and verification evidence.
