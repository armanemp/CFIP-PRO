# CFIP-PRO cTrader v72 — Phase 5 Structure / FVG / OB / Liquidity

Date: 2026-09-27
Repository: armanemp/CFIP-PRO
Base: integrations/ctrader/calude-edit-v71.cs
Source: integrations/ctrader/calude-edit-v72.cs

## Scope

This phase audits and hardens the structural market model used by decision, entry and target discovery:

- internal structure / BOS
- MSS / CHOCH
- displacement
- FVG
- Order Blocks
- liquidity sweeps
- equal highs/lows
- zone mitigation and age
- BUY/SELL symmetry
- historical discovery versus live execution selection

v71 remains preserved as the prior version. v72 is an independent file.

## Structural findings

### Structure symmetry

Bull/Bear implementations were reviewed as paired functions:

- BullStructure / BearStructure
- BullMss / BearMss
- BullChoch / BearChoch
- BullDisplacement / BearDisplacement
- BullLiquiditySweep / BearLiquiditySweep
- FindSwingHigh / FindSwingLow
- FindSwingHighAbove / FindSwingLowBelow
- FindEqualHigh / FindEqualLow

The core comparisons are directionally symmetric. No unilateral BUY/SELL branch was retained in this audit.

### FVG mitigation

The managed FVG builder already contained a correction for the historical full-breach resurrection defect: a fully swept zone is collapsed to zero width and excluded rather than restored to its original range.

v72 makes the execution contract explicit:

- historical/decision discovery can apply the configured current-retest filter
- execution discovery does not require the close of the latest historical candle to already be inside the FVG
- execution selection can rank the discovered zone using the current live market price
- a fully consumed FVG cannot become a fresh active zone

The legacy `FVG Invalidate On Full Fill` setting is now labeled `Safety-Enforced` because a full zone breach cannot safely resurrect a consumed zone.

### Order Block mitigation

Order Block discovery is now separated from historical retest gating for execution.

Execution discovery:

- uses current market price for zone-distance selection
- does not require a historical candle to have already retested the OB
- retains the existing mitigation checks so fully consumed OBs are still rejected

### Liquidity sweep depth

Bull/Bear liquidity sweeps now use an explicit penetration measurement against the prior extreme and a minimum depth:

`max(2 * pip-size, ATR * LiquiditySweepMinimumDepthAtr)`

The sweep detector itself does not read live Ask/Bid. Historical frame classification therefore remains independent of current spread.

The same depth contract is applied to the liquidity contribution inside Order Block quality scoring.

### OB/FVG confluence

Order Block quality scoring now checks FVG confluence through `BuildManagedFvgZone`. A fully mitigated FVG therefore cannot continue to inflate OB quality after its range has been consumed.

## Live versus historical boundary

The v72 structural contract is:

`Closed-bar analysis -> structural zone discovery -> live-price zone selection -> live execution gate`

Historical analysis must not be contaminated by live spread/price state.

Live execution must not discard a valid structural zone solely because the close of the decision candle was not yet inside that zone.

## Current structural inventory

- 1 FVG discovery function
- 1 FVG execution wrapper
- 1 Order Block discovery function
- 1 Order Block execution wrapper
- 1 managed FVG constructor
- 1 OB candidate builder
- 1 Bull liquidity sweep
- 1 Bear liquidity sweep
- 1 OB liquidity sweep scorer
- equal-high and equal-low detectors retained as paired implementations

## Remaining Phase 5 work

The structural phase is not yet considered final until:

1. zone identity and provenance are formalized instead of using only the mutable `Zone` object
2. a common `StructureSnapshot`/zone ledger replaces repeated ad-hoc discovery
3. BOS/MSS/CHOCH semantics are normalized into explicit event types
4. displacement definition is unified with impulse/quality logic
5. liquidity pools are modeled explicitly and linked to target/entry confluence
6. zone invalidation, mitigation, expiry and retest become explicit state rather than implicit helper behavior
7. FVG/OB candidate selection is centralized for both entry and target consumers
8. scenario/replay tests cover both BUY and SELL paths and partial/full mitigation boundaries

## Validation boundary

Static source checks passed while generating v72.

A real cTrader compile/runtime test has not yet been executed in a cTrader build environment. Runtime behavior and broker interaction therefore remain unverified at execution level.
