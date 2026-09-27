# CFIP-PRO cTrader v71 — Phase 3 MTF / Candle / Time Contract

Date: 2026-09-27
Repository: armanemp/CFIP-PRO
Base: integrations/ctrader/calude-edit-v70.cs
Source: integrations/ctrader/calude-edit-v71.cs

## Objective

Normalize the time/MTF contract before changing signal logic:

1. One M5 reference defines the decision snapshot.
2. Every MTF frame is evaluated on a fully closed bar at that reference.
3. Chart-level confluence never reads the forming chart bar.
4. Runtime/session/news/daily-loss timestamps use the cTrader server UTC clock.
5. User-facing local time uses the cTrader platform user offset.

## Findings from v70

The MTF frame calculation was already substantially guarded against look-ahead:

- M5 used `_m5Bars.Count - 2`.
- M1/M15/M30/H1/H4/D1/W1 used a `ClosedIndex(..., reference)` helper.
- `AnalyzeFrame` consumes an explicit bar index.
- `BuildDecision` receives the M5 closed index and a time reference.

However, `BuildDecision` also called `LiveBias(chartIndex, ...)`. On a chart timeframe higher than M5, the mapped chart index could refer to the currently forming chart bar. That created a potential look-ahead/confluence inconsistency even though the primary MTF frames were closed.

Separately, runtime code used `DateTime.UtcNow` for session, news, daily-loss, protection cooldowns, alerts and related timing. cTrader provides `TimeInUtc` as the current server time in UTC, while indicator bar times follow the indicator's `TimeZone` setting. v71 uses the cTrader server clock as the runtime authority. citeturn507219search0turn507219search8

## v71 changes

### A. Single M5 reference

`Calculate()` now derives the reference from the current M5 bar open time and derives the closed M5 index through `ClosedIndex(_m5Bars, reference)` rather than relying directly on `Count - 2`.

This makes the contract explicit and reuses the same closed-index algorithm used by the other timeframes.

### B. Closed chart confluence

Added:

- `ClosedChartIndex(reference, fallback)`
- `MapM5ToClosedChart(m5Index, alternate)`

When `BuildDecision()` evaluates `LiveBias`, it now maps the M5 decision timestamp to the last chart bar fully closed at that timestamp.

This removes the forming-chart-bar dependency.

### C. Server UTC runtime clock

All `DateTime.UtcNow` uses in v71 were replaced with cTrader's `TimeInUtc`.

This affects:

- session filtering
- Friday filtering
- news guard timing
- daily-loss circuit breaker timing
- broker-protection throttling
- trading-permission throttling
- alert timing
- popup timing
- outcome/runtime timestamps
- panel UTC time

### D. User-facing local time

The panel's local-time display is now derived from:

`TimeInUtc + Application.UserTimeOffset`

instead of the Windows process clock.

## Current MTF data map

At every newly closed M5 bar:

- M1 -> last M1 bar fully closed at the M5 reference
- M5 -> closed M5 bar
- M15 -> last M15 bar fully closed at the M5 reference
- M30 -> last M30 bar fully closed at the M5 reference
- H1 -> last H1 bar fully closed at the M5 reference
- H4 -> last H4 bar fully closed at the M5 reference
- D1 -> last D1 bar fully closed at the M5 reference when sufficient history exists
- W1 -> last W1 bar fully closed at the M5 reference when sufficient history exists

The primary decision engine therefore sees a temporally coherent cross-timeframe snapshot.

## Remaining Phase 3 work

The following still need to be handled before Phase 3 is fully closed:

- formalize an immutable `MtfSnapshot` object rather than assigning eight mutable frame fields directly
- verify all structure/zone helper methods respect their supplied index and do not internally substitute a live index
- audit every target, SL and trigger helper for hidden `Count - 1` reads
- separate live-price management from closed-bar signal calculations at the type/contract level
- add replay-style index tests for M1/M5/M15/M30/H1/H4/D1/W1 boundary conditions
- validate daylight/session boundary behavior around weekends and missing bars
- run a real cTrader compile/runtime test

## Validation boundary

The v71 source passed the repository-side structural checks used while generating the file, including version isolation and lexical balance.

A real cTrader compilation/runtime test has not yet been executed; therefore this document does not claim runtime compilation success.
