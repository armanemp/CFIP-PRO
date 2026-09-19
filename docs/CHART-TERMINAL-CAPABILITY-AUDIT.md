# CFIP-PRO Chart Terminal Capability Audit

Updated 2026-09-19.

## External capability baseline reviewed

The terminal target was reviewed against current public TradingView Supercharts and cTrader chart documentation. TradingView currently documents multi-chart layouts, synchronized symbols/timeframes/drawings, command search, custom intervals, volume-profile/footprint-oriented analysis, 400+ built-in indicators and strategies, 110+ drawing tools, Bar Replay, alerts, watchlists, templates, undo/redo, and extensive chart-management controls. cTrader documents chart types, indicator management, object management, templates, hide/show drawings, workspace/layout controls, multiple chart modes, chart viewing options, and a large built-in indicator catalogue.

## CFIP-PRO implementation boundary

The project does **not** copy proprietary TradingView/cTrader code or UI assets. Lightweight Charts 5.2.1 remains the rendering engine. CFIP owns its own terminal shell, state model, normalized-data contract, analytics, drawing model, risk model, persistence and provider integration.

## Current implemented foundation

- Chart-first native Windows terminal.
- Lightweight Charts 5.2.1 with required TradingView attribution enabled.
- Candlestick, OHLC bar, line, area and baseline chart modes.
- 1m, 5m, 15m, 30m, 1H, 4H, 1D, 1W and 1M timeframe selector.
- Modular symbol catalogue covering major, minor and exotic FX pairs.
- Symbol picker with search.
- 2-second polling of normalized market observations with client-side version gating to avoid redundant chart rebuilds when the market payload has not changed.
- Explicit no-synthetic-data empty state.
- EMA/SMA/WMA/VWAP/Bollinger overlay foundation.
- Oscillator studies (RSI, MACD, DMI/ADX, Stochastic) use a dedicated secondary indicator pane; volume uses its own separate secondary pane when enabled.
- Overlay studies remain on the main price pane.
- Measurement tool reports price delta, percentage change and actual candle distance (bar count) based on the current candle series.
- Bid/ask visibility control is wired to the latest normalized observation and shows available bid/ask values in the chart HUD.
- Watchlist entries are actionable and switch the active chart symbol.
- Risk inspector contains a client-side sizing estimator for equity, risk %, leverage, contract size, entry, stop and target, including risk cash, capped units, lots and R:R.
- Viewport preservation across indicator/chart/data refreshes rather than forced `fitContent` on every refresh.
- Drawing/FVG overlay coordinate refresh synchronized with time-scale pan/zoom and chart resize.
- Drawing selection, drag movement, lock/hide/delete, object selection from the inspector, undo/redo and keyboard deletion are wired.
- Crosshair, scroll, zoom, pinch and axis scaling.
- Modular tool rail.
- Modular inspector/sidebar with collapse/show controls.
- Market, watchlist, structure, intelligence, risk and objects inspector sections.
- Modular indicator/data/type/i18n controls.
- English/Persian/German/Arabic/Turkish/French/Spanish/Portuguese/Russian/Chinese/Japanese locale foundation with RTL support for Persian and Arabic.
- Correct attribution wording uses TradingView's public domain, `tradingview.com`; the built-in attribution logo remains enabled.

## Next implementation layers — explicitly not claimed complete yet

1. Real provider adapter(s) and websocket/tick streaming into the normalized market-observation contract.
2. Expand the drawing object model with endpoint handles and per-tool resize semantics (current implementation supports selection/move plus lock/hide/delete and undo/redo).
3. Full TradingView/cTrader-class drawing catalogue: channels, pitchfork/Gann, Fibonacci variants, patterns, text/annotations, measurement and position tools.
4. Full indicator engine parameter dialogs, per-indicator parameter persistence, pane-specific scaling and richer oscillator visualization.
5. Market structure and intelligence rendering: BOS/CHoCH/MSS, liquidity, FVG/IFVG, OB/Breaker/Mitigation, sessions/kill zones and MTF confluence.
6. Replay engine with deterministic historical cursor, play/pause/step/speed and return-to-live.

## Latest hardening pass — 2026-09-19

- Separated oscillator and volume panes so volume no longer shares the oscillator scale.
- Corrected Measure bar count to use candle indices rather than raw Unix timestamp seconds.
- Wired Bid/Ask preference to real normalized observation fields when available.
- Made the built-in watchlist switch the active symbol.
- Added a local risk-sizing estimator with explicit account inputs and broker/execution-layer caveat.
- Kept provider streaming, replay, alerting, multi-chart, advanced drawing handles and account integration explicitly outside the current completion claim.
7. Alert engine with price/drawing/indicator conditions and notification delivery.
8. Multi-chart layouts, synchronization groups and workspace persistence.
9. Full scale controls: left/right scales, percent/indexed/log, precision, timezone, bar spacing and offsets.
10. Connect the risk/position estimator to broker/account state, contract specifications and account-currency conversion; current sizing is explicitly an offline estimate.
11. Command palette and keyboard-shortcut registry.
12. Chart templates and saved indicator/layout profiles.

These are development layers, not permission to replace the existing greenfield architecture with TradingView/cTrader code.
