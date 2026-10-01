# CFIP Chart Terminal Rebuild Execution

## Objective

Rebuild the CFIP professional chart terminal as a modular production component instead of a single growing component.

## Scope

- TradingView Lightweight Charts™ based rendering layer
- Correct attribution and licensing placement
- Real-time market data provider abstraction
- Forex symbol universe and watchlist
- Modular toolbar and drawing tools
- Indicator engine
- Chart preferences and persistence
- Multi-language and RTL support
- Professional terminal layout inspired by TradingView and cTrader workflows

## Implementation order

1. Stabilize current chart engine and remove technical debt.
2. Split UI modules into independent components.
3. Add provider adapter interface and demo provider.
4. Implement live candle update pipeline.
5. Expand terminal controls and drawing system.
6. Add validation, tests and documentation.

## Rules

- No placeholder UI pretending to be a finished feature.
- Every module must remain independently maintainable.
- Every completed stage must pass typecheck and lint before merge.
