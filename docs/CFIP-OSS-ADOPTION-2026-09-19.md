# CFIP-PRO OSS Adoption Matrix — 2026-09-19

This document records the current integration strategy. OSS is adopted through CFIP-owned
contracts and adapters; installing a package is never treated as feature completion.

## Terminal and data references

- TradingView Lightweight Charts remains the chart rendering engine and supports custom
  plugins, so CFIP keeps the renderer stable and owns the terminal state/object model.
- OpenTerminalUI was reviewed for multi-panel terminal workflows, provider waterfall,
  screening, portfolio, alerts, replay and AI research ideas.
- Open Exchange trading UI was reviewed for order-book, tape, order-entry and execution
  surface decomposition.
- OpenBB was reviewed as a broad financial-data integration candidate for a future adapter.
- Pairlens was reviewed for plugin registry, AI-provider and local/offline architecture ideas.

## Adoption rules

1. Prefer permissive licenses compatible with CFIP distribution.
2. Verify Python 3.14 / Node runtime compatibility before adding dependencies.
3. Wrap every adopted component behind a CFIP interface.
4. Preserve normalized market-data, analysis, risk and intelligence contracts.
5. Reject OSS that forces vendor-specific state into the core domain.
6. Record license, maintenance, security, performance and semantic-equivalence evidence.
7. Keep a replaceable adapter so the product remains portable.

## Current decisions

- **Integrated:** Lightweight Charts, TA-Lib, CCXT, Polars, pyvsmc, PydanticAI.
- **Adapter/reference evaluation:** OpenBB, OpenTerminalUI, Open Exchange trading UI,
  Pairlens and other terminal/research projects.
- **Do not blindly import:** UI code, provider-specific models, execution semantics,
  proprietary assets or unverified dependencies.

The implementation target is capability parity through modular contracts, not repository
copying.
