# CFIP-PRO cTrader v78 — Phase 5 Structure / FVG / OB / Liquidity Ledger

Date: 2026-09-27
Baseline: v77 Market Model + audited v72/v73 structural behavior
Implementation: integrations/ctrader/calude-edit-v78.cs
Phase: 5 — Structure, FVG, OB and Liquidity ledger

## Objective

v78 replaces repeated structural rediscovery with one canonical structural ledger. Structure, zones, liquidity and premium/discount are calculated once from the canonical closed MTF snapshot and exposed to later Signal, Entry, Stop and Target consumers.

## Structural coverage

- BOS, MSS, CHOCH and confirmed swing events
- displacement
- FVG creation, retest, partial mitigation, expiry and non-resurrection after full consumption
- Order Block discovery, displacement requirement, body/full-range mode, retest, breach and expiry
- equal-high/equal-low liquidity
- liquidity sweeps with explicit ATR penetration depth
- swing liquidity
- prior day/week highs and lows
- configured session high/low
- daily pivot/R1/R2/S1/S2
- premium/discount state
- zone-to-FVG and zone-to-liquidity confluence
- typed provenance and stable object identity

## Canonical ownership

Downstream consumers now query StructureSnapshot rather than independently rebuilding FVG/OB/liquidity. This is the required foundation for the final Signal and Entry/Trigger layers.

## Trading integrity

Phase 5 does not decide the final BUY/SELL signal. StructureDirection is context. The Phase 6 Decision Engine will combine MarketModel evidence, MTF, structure and confluence into the authoritative DecisionSnapshot.

Automatic market trading, automatic pending orders, smart SL/TP and broker protection remain downstream execution concerns. No broker mutation or UI authority was added here.

## Non-resurrection rule

A fully consumed FVG becomes Consumed and its usable bounds are cleared. It cannot silently return as a fresh active FVG later.

## Validation

Repository/source checks confirm 512/512 parameter parity with v77, one canonical ledger, typed lifecycle/events, FVG non-resurrection, OB mitigation controls, explicit liquidity pools/sweeps, prior day/week/session/pivot levels, closed-MTF inputs, and absence of broker/chart authority.

Current validation is source-level. Real cTrader compile/runtime remains a Phase 16 gate.

## Phase 5 acceptance

COMPLETE at the architecture/structural-contract level.

Next phase: Phase 6 — Decision engine
