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
- Volume histogram in a dedicated secondary pane.
- RSI, MACD, DMI/ADX and Stochastic in a dedicated secondary indicator pane; overlay studies remain on the main price pane.
- Viewport preservation across indicator/chart/data refreshes rather than forced `fitContent` on every refresh.
- Drawing/FVG overlay coordinate refresh synchronized with time-scale pan/zoom and chart resize.
- Crosshair, scroll, zoom, pinch and axis scaling.
- Modular tool rail.
- Modular inspector/sidebar with collapse/show controls.
- Market, watchlist, structure, intelligence, risk and objects inspector sections.
- Modular indicator/data/type/i18n controls.
- English/Persian/German/Arabic/Turkish/French/Spanish/Portuguese/Russian/Chinese/Japanese locale foundation with RTL support for Persian and Arabic.
- Correct attribution wording uses TradingView's public domain, `tradingview.com`; the built-in attribution logo remains enabled.

## Next implementation layers — explicitly not claimed complete yet

1. Real provider adapter(s) and websocket/tick streaming into the normalized market-observation contract.
2. Full drawing object model: selection, handles, move/resize, lock/hide, object tree, undo/redo and persistence.
3. Full TradingView/cTrader-class drawing catalogue: channels, pitchfork/Gann, Fibonacci variants, patterns, text/annotations, measurement and position tools.
4. Full indicator engine parameter dialogs, pane-specific scaling and richer oscillator visualization beyond the current shared secondary pane.
5. Market structure and intelligence rendering: BOS/CHoCH/MSS, liquidity, FVG/IFVG, OB/Breaker/Mitigation, sessions/kill zones and MTF confluence.
6. Replay engine with deterministic historical cursor, play/pause/step/speed and return-to-live.
7. Alert engine with price/drawing/indicator conditions and notification delivery.
8. Multi-chart layouts, synchronization groups and workspace persistence.
9. Full scale controls: left/right scales, percent/indexed/log, precision, timezone, bar spacing and offsets.
10. Account-aware risk/position overlays connected to broker/account state.
11. Command palette and keyboard-shortcut registry.
12. Chart templates and saved indicator/layout profiles.

These are development layers, not permission to replace the existing greenfield architecture with TradingView/cTrader code.
