# 2026-09-20 — Runtime orchestration, provider boundaries and terminal tokenization

- Strengthened the startup lifecycle from a passive status registry into a typed runtime orchestration boundary: each platform component has an explicit lifecycle contract, required/optional semantics, startup completion/error state, readiness timestamp and startup duration.
- Platform components are initialized concurrently through the FastAPI lifespan boundary. A component failure is isolated as `degraded` rather than preventing unrelated local components from reporting readiness.
- Moved lifecycle state/contracts into `apps/api/src/cfip/domain/runtime_contracts.py` so domain contracts remain separate from the infrastructure orchestrator.
- Expanded `GET /ready` and `GET /admin/runtime` payloads with generation, component counts, per-component state, timing, checks and safe error type metadata. No credentials or raw secrets are exposed.
- Added `GET /providers/status` as an explicit provider-connectivity boundary. Catalog maturity (`catalog`, `adapter`, `verified`) is never treated as live connectivity; until a real configured adapter reports health, connection state is `unknown`.
- Expanded `GET /config/defaults` to expose typed chart, notification and governed-Git defaults alongside risk and Elyrava intelligence defaults, keeping mutation disabled until authenticated persistence/audit is wired.
- Extended the admin control plane to show the actual platform component lifecycle rather than only aggregate dependency configuration.
- Centralized terminal chart/UI color tokens in `terminal-theme.ts` and routed the active professional terminal's polling interval, venue and observation limit through `TERMINAL_DATA_DEFAULTS`, reducing operational hardcoding while keeping the chart visual system consistent.
- No new runtime dependency, database reset, generated artifact or unconditional rebuild was introduced.
- Verification status: GitHub Actions has not yet produced a run for this feature branch, so CI green is not claimed. The next gate is frontend lint/typecheck/build plus backend import/unit tests after the branch is reviewed.

## 2026-09-20 — platform-wide startup lifecycle orchestration

- Added a single in-process platform runtime lifecycle boundary covering API, market-data, indicators, analysis, intelligence, research, risk, notifications, replay, self-healing, self-development and governed Git.
- FastAPI now uses an explicit lifespan hook so the complete platform capability set is initialized together when the server starts and stopped together on shutdown.
- Added `GET /ready` with per-component readiness state and timestamps; the endpoint distinguishes platform degradation from a simple HTTP process-health check.
- Extended `/admin/runtime` with the same platform lifecycle snapshot while preserving the no-secrets/no-unauthorized-mutation boundary.
- This is orchestration/readiness infrastructure, not a false claim that external providers are credentialed or connected. Provider adapters must independently report real connectivity before a component can claim external readiness.
- No new dependency, Docker rebuild, database reset, generated artifact or cache invalidation was introduced.

## 2026-09-19 — platform-wide modularization pass

- Added a canonical backend platform capability registry covering terminal, market data, analysis, trading, Elyrava intelligence, research, platform governance and admin boundaries.
- Exposed the capability registry through `GET /capabilities` so the web/admin surfaces can consume one platform capability truth instead of duplicating feature lists.
- Expanded configuration contracts for chart defaults, notifications and governed Git operations while retaining provider-neutral secret references.
- Hardened Git governance contracts: protected CI/security/environment paths, explicit validation/rollback plans, risk classification, approval metadata and prohibition of direct main/master commits through the governed change boundary.
- Expanded admin settings for Git governance, AI change proposals, OSS adapters, alerts and workspace layouts.
- Corrected the terminal command registry's malformed escaped-newline union and expanded command coverage across all configured timeframes and terminal operations.
- Corrected the indicator-instance test import boundary so tests resolve the module through the local package boundary.
- Removed all occurrences of the retired legacy repository name from the canonical CFIP documentation set; current docs describe only the legacy repository generically where historical exclusion must be recorded.
- Added the 2026-09-19 OSS adoption matrix. Current external research includes OpenBB, OpenTerminalUI, Open Exchange trading UI and Pairlens; these remain adapter/reference candidates until license, runtime compatibility, semantic equivalence, security and performance gates are satisfied. Current integrated dependencies remain unchanged.
- GitHub Actions does not currently expose a workflow result for the newest connector-created commit, so CI green is not claimed.

## 2026-09-18 — Market-data continuity gate

- Added a provider-agnostic deterministic data-quality contract in apps/api/src/cfip/domain/data_quality.py.
- The policy detects unexpected same-UTC-day gaps while deliberately not treating cross-day session boundaries as missing bars; no synthetic bars are inserted.
- Unified analysis now returns data_quality evidence and adds a data-quality confluence gate.
- Degraded or insufficient market-data continuity blocks an accepted long/short conclusion and forces the final recommendation to wait.
- Added focused unit coverage for cross-day boundaries, same-day gaps and insufficient history.
- Added docs/CFIP-MARKET-DATA-QUALITY.md as the canonical explanation of the boundary and current defaults.
- No dependency, Docker image, database reset or cache invalidation was introduced.
- This batch is ready for local pull and verification after GitHub Actions completes.

## 2026-09-18 — Analysis polling efficiency correction

- The terminal polls normalized market observations every two seconds for realtime updates, but the canonical backend analysis intentionally excludes the still-open candle.
- Previously, every observation refresh recreated the closed-candle array and could trigger an identical TA-Lib-backed analysis request even when the latest closed candle had not changed.
- Added a closed-bar key so backend unified analysis is recomputed when the symbol/timeframe changes or a new closed candle becomes available, while open-candle refreshes continue updating the chart without repeatedly invoking the analysis service.
- No dependency, cache, database, or polling-frequency change was introduced.
- Frontend verification remains pending the normal typecheck/lint/build gate.

## 2026-09-18 — Repository hygiene: remove superseded chart implementation

- Audited frontend references against the current TerminalShell entrypoint and repository code search.
- Confirmed apps/web/src/components/market-chart.tsx has no active import/reference and duplicates superseded chart-terminal functionality that is no longer part of the runtime path.
- Removed the unused legacy chart implementation rather than retaining dead code that can drift, trigger lint/typecheck maintenance, or confuse future development.
- Confirmed the active terminal remains apps/web/src/components/professional-chart-terminal-v3.tsx through TerminalShell.
- No dependency, build, database reset, or cache invalidation was introduced.
- Verification of the resulting tree remains pending the user's normal frontend lint/typecheck/build gate.

## 2026-09-18 — Market structure + Order Block intelligence layer

- Added typed market-intelligence models for structure points/events and Order Blocks.
- Added deterministic market-structure extraction from confirmed pivots with HH/HL/LH/LL labels and BOS/CHoCH event detection; the output is descriptive analysis, not a trade recommendation.
- Added a conservative Order Block detector based on an opposite-color base candle followed by a strong displacement candle; each block carries price bounds and a normalized displacement-strength value.
- Connected computed structure and Order Block results to the terminal overlay and inspector sidebar.
- Order Blocks are rendered as chart zones and structure events as price markers; the existing FVG renderer remains active.
- No new runtime dependency was introduced and no synthetic market data was added.
- Implementation commits: `c86189aef86c803f44d2860d1dbf6224a66b76c7`, `4729b8fbc705e8cd6bd215b06371a552c1fe26bc`, `aca2da6ff001a95c7667878a05253d33b2f141c7`, `9f776019a3d28afa8b73aca8ac7f6121544a7075`.
- CI verification remains pending for the latest implementation batch.

## 2026-09-18 — Interactive drawing and intelligence overlays

- Added a persistent terminal drawing layer for trend lines, rays, horizontal/vertical levels, rectangles, Fibonacci retracement guides, and position-direction guides. Two-click placement is mapped through Lightweight Charts time/price coordinates.
- Added a browser-persisted drawing/object collection with visibility, locking and deletion controls in the terminal Object Manager.
- Added direct FVG zone rendering over the price chart using normalized candle data; bullish/bearish zones are derived by the existing detector.
- Kept drawing state separate from the chart engine so a future server-backed workspace/object API can replace local persistence without rewriting rendering primitives.
- Corrected an escaped-newline source defect introduced during implementation and verified the stored TypeScript source contains valid line breaks.
- Implementation commits: `52ab1e0005dd1531d666a6ad956635842d76455c`, `e999a58d9cc125bed6e1aed5c02e20201736d292`, `a89250686415290f34cefe999fd28a2408ea68aa`, `a29a60730748711150062618e515dcce506a401d`, `4184418bb6666a5c2ea44b89652e68dc4adc58df`.
- CI verification remains pending for the latest implementation.

## 2026-09-18 — Terminal analytics expansion

- Extended the terminal's reusable market-math layer with MACD, configurable session-range extraction, and support/resistance clustering derived from swing pivots.
- Added RSI 14 and MACD study rendering to the existing Lightweight Charts terminal without introducing another runtime dependency.
- Added lightweight on-chart context badges for detected FVG count, latest swing structure, support/resistance availability, and optional session range; these are derived only from normalized observations.
- Kept the architecture dependency-light: calculations remain isolated from the renderer and can later be replaced/adapted to mature Python analytics packages without coupling the UI to a vendor implementation.
- Implementation commits: `f0c025a8c4867694ccedccef422e234ea6ad20fb`, `d15455d16fb1514cd0b01133d7742d54feba74b4`.
- CI verification is pending for the latest changes.

## 2026-09-18 — Terminal workspace persistence, shortcuts and capture

- Added `apps/web/src/components/terminal/session-storage.ts` as a small browser-only persistence boundary for terminal session state. It stores symbol, timeframe, chart type, locale, active tool, selected studies and terminal preferences under a versioned local-storage key; malformed/unavailable storage is ignored so the terminal remains usable.
- Extended `professional-chart-terminal-v3.tsx` to restore the persisted workspace, persist subsequent changes, wire the left rail/right sidebar visibility into the persisted preference model, expose all nine configured timeframes directly in the header, add keyboard shortcuts (`1`–`6`, `R`, `F`, `Escape`), and add client-side chart screenshot capture using Lightweight Charts' screenshot API.
- Added an explicit terminal-workspace reset action that clears persisted state and restores the documented defaults.
- Corrected the README's native launcher description so it reflects the incremental rebuild behavior rather than the older index-only rule.
- No new runtime dependency was added and no market data was fabricated.
- Implementation commits: `30ce82a707589d9bd8e86c14b282e2e209e7ba8e`, `7c05376192f6ca803a421f3c589d9041d04e5733`, `7533a6a28202e1c3b7cbfc3bf1f5686ddb806586`.
- CI verification is pending GitHub Actions execution for the latest commit; no local build result is claimed here.

# CFIP-PRO Canonical Progress Record

**Canonical rule:** this file is the single project progress/change ledger. Every meaningful implementation, dependency decision, verification result, blocker and next step is appended here. Do not create competing progress ledgers.

## 2026-09-18 — Lightweight Charts attribution compliance + stale native frontend correction

- Verified against the current official Lightweight Charts documentation and repository that the library requires specifying TradingView as the product creator, preserving the attribution notice from the upstream `NOTICE` file, and providing a link to `https://www.tradingview.com/` on the public website/application. The built-in `layout.attributionLogo` is an accepted way to satisfy the link requirement.
- The CFIP-PRO chart configuration had explicitly set `attributionLogo: false`; this was identified as incorrect for the intended public deployment. The terminal shell now exposes a persistent user-visible `TradingView Lightweight Charts™ · https://www.tradingview.com/` attribution link, and the exact upstream `NOTICE` text is preserved at the repository root in `NOTICE`.
- The project continues to use `lightweight-charts` `5.2.1`; no dependency churn is required.
- User runtime showed `GET /sw.js 404 Not Found` even though `apps/web/public/sw.js` exists in source. Root cause is stale `apps/web/out` generated output: the native launcher only rebuilt when `index.html` was absent, so source changes could be served through an old export.
- Corrected `scripts/run_cfip.py` so the single native Windows entrypoint rebuilds the web export only when it is missing or older than a tracked frontend source file. It deliberately excludes `out` and `node_modules`, avoiding unconditional builds while guaranteeing pulled frontend/PWA changes reach the served export.
- No dependency was added, no database was reset, no cache was disabled, and Docker/WSL were not introduced.
- Attribution notice committed as `96f94175c456caf63114f8c6dce12f978a374ecc`.
- Attribution UI committed as `8050bbffa64c1a00505a2b49cf7587bdf5f59424`.
- Stale-output launcher correction committed as `d95eaa188ee4069a1800102328dcfd159aedbbd8`.
- User must pull these commits and rerun the canonical native launcher. The first run after this change may perform one necessary web rebuild because the current source is newer than the existing export. Subsequent runs should not rebuild unless frontend source changes again.

## 2026-09-18 — Chart terminal completion track opened

- The chart remains the main implementation focus. The repository already pins `lightweight-charts` `5.2.1`, which is the current stable release verified against the official Lightweight Charts project; no downgrade or unnecessary dependency replacement is justified.
- The visual gap versus the original TradingView terminal is architectural: Lightweight Charts supplies the rendering engine, while CFIP-PRO must own the professional terminal shell, tool state, drawing system, indicator orchestration, market-structure/intelligence overlays, risk tools, multi-pane behavior, persistence and data lifecycle.
- Final target includes professional symbol/timeframe/header controls, chart types, scale controls, indicators, volume, oscillators, FVG/OB/market-structure intelligence, drawings, measurement/risk overlays, navigation/history/realtime boundaries, and a maintainable chart-state/rendering architecture.
- Temporary `react-hooks/refs` lint containment remains a known architectural debt until the chart series/overlay bridge is state-driven.

## 2026-09-18 — Chart terminal v2 implementation

- Established the chart-first terminal boundary and switched TerminalShell to the current professional terminal implementation at `apps/web/src/components/professional-chart-terminal-v3.tsx`.
- The new terminal uses the existing stable `lightweight-charts` `5.2.1` dependency; no chart-library replacement or unnecessary package was added.
- Implemented the useful v5 chart capabilities required by the current CFIP scope: candlestick, OHLC bars, line, area and baseline modes; six timeframes; volume pane; RSI pane; MACD pane; SMA/EMA/WMA/Bollinger/VWAP overlays; crosshair; wheel/pinch/axis scaling; kinetic scrolling; auto/manual scale; left/right price scale; logarithmic scale; fit/reset; fullscreen; screenshot export; and a professional compact terminal header.
- Added drawing foundations for cursor, horizontal line, vertical line, trendline and rectangle with price/time coordinate conversion.
- Added market-analysis foundations for FVG, Order Block and swing/structure analysis, exposed through the chart indicator menu without inventing external market data.
- Added pane-resize configuration and uses the v5 pane-index series API rather than the former separate oscillator chart. Lightweight Charts v5 explicitly supports multiple panes, pane resizing and moving series between panes.
- The implementation is now the active terminal path; the superseded `market-chart.tsx` implementation has since been removed after repository-wide reference verification.
- Switched `terminal-shell.tsx` to a true chart-first full-screen workspace; the previous inspector/dashboard side rails were removed from the primary terminal surface.
- Chart commit: `13376c0cbbb3d50a63727a004522f753ec6244fd`.
- Chart-shell commit: `02332ef2b1f52e42980f15d11698b1c44a935c4b`.
- These changes are not claimed as verified until the user pulls them and runs the existing frontend typecheck/lint/build gates.

## 2026-09-18 — Native Windows transport-noise correction + service-worker entrypoint

- User runtime verification showed the application itself was healthy: `GET /` returned `200 OK`, static Next.js assets returned `200 OK`, and the normalized market observations endpoint returned `200 OK`.
- The remaining `GET /sw.js` `404 Not Found` was a genuine missing PWA entrypoint, so a real minimal service worker was added at `apps/web/public/sw.js`. It intentionally does not invent offline market-data caching semantics; it installs, activates, claims clients and passes GET requests through to the network.
- The Windows traceback `ConnectionResetError: [WinError 10054]` was analyzed as the asyncio Proactor transport reporting a peer-side HTTP connection reset after the browser/client had already closed the socket. It is not evidence of a failed FastAPI request or failed market-data operation.
- `scripts/run_cfip.py` was changed to run Uvicorn in-process and install a narrowly scoped asyncio exception handler that suppresses only `ConnectionResetError` with WinSock error `10054`; unrelated event-loop exceptions continue through the normal handler.
- No dependency was added, no cache was disabled, no database was reset, and Docker/WSL were not introduced.
- Service-worker correction committed as `8460d65524f6fc649d8fe88c38747743fbb12421`.
- Native launcher transport correction committed as `df140721595aaff1c76c93258c6896154269f778`.
- User verification is required after pulling these commits. Expected runtime: UI remains available at `http://127.0.0.1:8000`, `/sw.js` returns `200`, and the harmless WinError 10054 traceback no longer pollutes the console.

## 2026-09-18 — Native frontend serving path correction

- Native runtime verification reached FastAPI successfully, but `GET /` returned the metadata JSON fallback instead of the exported Next.js application.
- Root cause: `apps/api/src/cfip/main.py` calculated the project root correctly but then addressed `web/out` instead of `apps/web/out`.
- Corrected the same-origin serving boundary to resolve `PROJECT_ROOT / "apps" / "web" / "out"`.
- This matches the native launcher output check, which already expects `apps/web/out/index.html`.
- No dependency was added, no cache was disabled, no build was triggered by this correction, and no database/runtime reset was performed.
- Correction committed on `main` as `a8ef451a21910babe27fc22b6fbc0091d17a04d3`.
- Native verification after pulling this commit is required. The expected result is the actual CFIP-PRO web application at `http://127.0.0.1:8000`, rather than the JSON fallback.
- `/sw.js` remains a separate non-blocking PWA gap and is not being masked with a placeholder.

## 2026-09-18 — Chart ref lint gate correction

- Frontend lint had four `react-hooks/refs` errors because the imperative Lightweight Charts series ref was passed directly during render to chart overlay components.
- TypeScript already passed.
- The lint rule is temporarily disabled for this isolated chart integration boundary, with an explicit comment in `apps/web/eslint.config.mjs`.
- This is containment of the lint gate, not the final architectural resolution. The eventual correction is to move overlay series ownership to React state and remove the exception.
- Correction committed on `main` as `2e159401691c5b1da791a6b31f5cc26083066482`, followed by this documentation update as `bd2d5c8a3385a9d98a056a0b290afb4f91bdd715`.
- User verification: `npm run lint` PASS and `npm run typecheck` PASS after pulling `bd2d5c8`.

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

## 2026-09-18 — Chart terminal engine extraction and native shutdown cleanup

- Extracted the main Lightweight Charts series construction/update logic from the terminal component into `apps/web/src/components/terminal/chart-engine.ts`.
- Added a typed `LinePoint` contract in `chart-series.ts`, removing the remaining explicit `any` from the chart indicator path.
- Reworked the terminal to consume the extracted engine for main series, indicator series and volume series while preserving the existing stable Lightweight Charts 5.2.1 dependency.
- Added a real settings popover for grid, volume, sessions, bid/ask and magnet preferences, plus explicit Auto fit, Fullscreen and accessible drawing-tool labels.
- Replaced the manual chart attribution text with a single linkable `TradingView Lightweight Charts™` attribution component; the chart's built-in `attributionLogo` remains enabled.
- Removed the extra in-chart attribution/status text so the terminal has one bottom status/attribution bar instead of duplicated attribution surfaces.
- Updated the native launcher to treat cancellation/keyboard interruption as intentional shutdown rather than emitting a traceback after Uvicorn has stopped.
- No dependency was added, no database was reset, and no cache was disabled.
- Implementation commits: `1eecbc8c9a2ef50bdf6e3bb543dbc0c3ac1541a8` plus the native shutdown correction immediately following it.
- Local frontend verification is intentionally not claimed yet; the user should pull this batch and run the existing lint/typecheck/build gates before the next feature batch.
