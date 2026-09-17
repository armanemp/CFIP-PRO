# CFIP-PRO Canonical Progress Record

**Canonical rule:** this file is the single project progress/change ledger. Every meaningful implementation, dependency decision, verification result, blocker and next step is appended here. Do not create competing progress ledgers.

## 2026-09-17 — Native Windows runtime boot correction

- The frontend production build now completes successfully through Next.js 16.3.3: compilation, TypeScript, page-data collection, static generation and finalization all passed.
- The launcher then reached the FastAPI process and exposed a backend import-time failure in `apps/api/src/cfip/main.py`.
- Root cause: the `/` route is intentionally a union of `FileResponse | dict[str, str]` because the same-origin entrypoint either serves the built web `index.html` or returns the metadata fallback. FastAPI attempted to construct a Pydantic response model from that union and rejected `FileResponse`.
- Corrected the root route with `response_model=None`, preserving the runtime union while disabling invalid automatic response-model generation for this endpoint.
- No dependency was added, no cache was disabled, and no database/runtime reset was performed.
- Correction committed on `main` as `7bdc56b08133908f4733901685780e423ef51ff5`.
- Native runtime verification is still required after pulling this commit.

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

## 2026-09-17 — Native runtime verification + CI lint blocker

- User pulled `71e6577708709ca3d5caba396948b5b33f164721` and executed the canonical native launcher successfully.
- FastAPI application startup completed successfully under Uvicorn on `127.0.0.1:8000`.
- The same-origin root request returned `GET / HTTP/1.1` with `200 OK`, confirming the corrected root response-model boundary works in the real native runtime.
- Browser requested `/sw.js` and received `404 Not Found`. This is a non-blocking PWA gap; no placeholder Service Worker is being added merely to suppress the request.
- GitHub Actions run `#60` for `71e6577708709ca3d5caba396948b5b33f164721` has backend PASS, but frontend fails at `npm run lint`; therefore CI is not green and production readiness is not claimed.
- Inspection identified the existing anonymous default export in `apps/web/postcss.config.mjs` as a known lint warning source. Corrected the configuration to use a named `config` constant before default export.
- Correction committed on `main` as `f3a4ed3cf7c43839b76f0cf94dc919a1effab426`.
- No dependency was added, no cache was disabled, and no database/runtime reset was performed.
- Fresh CI verification is required for the new commit.

## Current stage

**Stage:** Native runtime stabilized → CI gate cleanup → chart terminal hardening

**Foundation:** implemented and previously locally verified

**Market data vertical slice:** implemented and previously locally verified

**PostgreSQL schema/migration:** implemented and verified with real PostgreSQL

**Transactional outbox:** implemented and locally verified

**NATS JetStream publisher/consumer boundaries:** implemented; live native runtime verification remains pending

**Market API:** implemented

**Frontend market-data API client:** implemented with Zod validation

**Chart module:** interactive terminal foundation implemented; canonical historical OHLC semantics, realtime streaming, MTF synchronization, persistent drawings and replay/backtest parity remain future vertical slices

**Single entrypoint:** frontend build PASS and native FastAPI runtime PASS on port 8000

**CI:** backend PASS; frontend lint failure identified and corrected; fresh CI pending

**Production readiness:** not claimed

## Verification state

**Python 3.14.7:** PASS

**Backend import/boot:** PASS — native Windows launcher reached successful FastAPI startup

**Root `/`:** PASS — native request returned HTTP 200

**Mypy:** PASS — no issues found in 26 source files

**Ruff:** PASS — all checks passed

**Full pytest:** PASS — 7 passed, 2 warnings

**Real PostgreSQL integration:** PASS — 1 passed

**PostgreSQL schema:** PASS — Alembic `0001_market_data`

**Frontend production build:** PASS — Next.js 16.3.3 compilation, TypeScript, page-data collection, static generation and finalization completed successfully

**Frontend typecheck in CI:** PASS

**Frontend lint in CI:** FAILED on commit `71e6577`; configuration correction committed as `f3a4ed3`; fresh verification pending

**Chart implementation:** COMMITTED — latest TypeScript compatibility correction committed

**Single port 8000 entrypoint:** PASS — native runtime verified

**Service Worker:** NOT IMPLEMENTED — `/sw.js` 404 is currently a known PWA gap, intentionally not masked with a placeholder

**Production readiness:** not claimed

## Next execution order

1. Pull latest `main` containing `f3a4ed3cf7c43839b76f0cf94dc919a1effab426`.
2. Verify frontend lint locally; CI will independently verify the correction.
3. Check fresh GitHub Actions result before declaring the CI gate green.
4. Run backend gates after the pull if local state is clean.
5. Continue chart hardening with true price/time-anchored FVG and Order Block overlays rather than screen-space placeholders.
6. Replace screen-space drawings with persistent price/time anchored drawing state.
7. Correct canonical historical OHLC aggregation semantics so open/close are based on timestamp ordering and volume semantics are explicit.
8. Add realtime observation streaming through the existing outbox/JetStream boundary without introducing fake market data.
9. Add MTF synchronization and indicator lifecycle management.
10. Establish replay/backtest parity against the same market-event/candle contracts.
11. Continue capability-by-capability OSS evaluation before adding specialized libraries.
12. Keep this file as the sole progress ledger.

## Operational rules

- CFIP-PRO is a clean greenfield project. The only external project used for capability reference is CForex.
- `cforex-platform` is permanently abandoned and must not be used as an architecture or migration baseline unless explicitly reinstated by the user.
- Native Windows is the current development workflow.
- Docker and WSL are not part of the normal execution loop.
- All Python execution uses the project `.venv`; canonical executable: `C:\Users\armanemp\Desktop\CFIP-PRO\.venv\Scripts\python.exe`.
- Port `8000` is the canonical user-facing application entrypoint. Normal operation must not require a second terminal or manually started frontend server.
- Do not add dependencies or rebuild artifacts unless evidence shows they are required.
- Do not claim production readiness or feature completeness without implementation and verification evidence.
