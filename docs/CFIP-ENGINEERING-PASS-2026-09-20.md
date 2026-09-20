# CFIP-PRO engineering pass — 2026-09-20

This pass continues the modularization track without using the abandoned `cforex-platform` repository.

## Implemented

- Added a canonical provider-neutral risk contract extension while preserving the existing broker-aware risk API.
- Added leverage-aware quantity capping to the existing `RiskService`; a position is never sized beyond account leverage or broker quantity limits.
- Added unit coverage for leverage-capped sizing and retained explicit-context/conversion/stop safety checks.
- Added `intelligence_governance.py` as a dedicated Elyrava self-development contract boundary: evidence references, validation plans, promotion status, rollback conditions and learning outcomes are explicit and auditable.
- Added deterministic governance helpers for human approval and promotion. The helper does not execute Git, shell commands, deployments or production mutations.
- Added `terminal-workspace.ts` as the canonical typed workspace state/reducer boundary so symbol, timeframe, chart mode, studies, drawings, panels, preferences and replay state can be separated from presentation code.
- Corrected the realtime stream decision type so its declared union exactly matches the decisions actually returned by `classify_event`.
- Removed an accidentally duplicated risk calculator/test rather than retaining parallel implementations of the same domain logic.

## Architecture rule reinforced

The terminal remains chart-first and presentation-driven; domain calculations stay in replaceable modules. Provider catalogs remain configuration metadata until a real adapter reports connectivity. Elyrava can observe, research, diagnose, propose and validate, but governed approval and promotion remain separate boundaries.

## Next vertical work

1. Wire the canonical terminal workspace reducer into the V3 shell and split chart lifecycle, drawing interaction and overlay rendering into focused modules without changing behavior.
2. Replace remaining terminal configuration literals with typed preference/theme tokens and complete locale coverage.
3. Add backend analysis/indicator/intelligence/risk adapter contracts to the same canonical boundary and connect realtime events to the frontend through the stream contract.
4. Build durable evidence/proposal/validation records and the admin approval queue, then connect approved changes to the existing governed Git proposal flow.
5. Complete data-driven Home/Admin surfaces and provider settings without inventing live connectivity.
6. Continue whole-repository import, test, boot, security, performance and missing-artifact audits before any merge claim.

## Verification status

GitHub repository write access is confirmed. No CI result is inferred from code changes; the branch must still receive an actual workflow run before CI is described as green.
