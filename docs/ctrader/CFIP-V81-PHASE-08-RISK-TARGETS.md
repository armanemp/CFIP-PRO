# CFIP-PRO cTrader v81 — Phase 8 Risk / Stop / Reward

Date: 2026-09-27

## Reference

- Parent implementation: `integrations/ctrader/calude-edit-v80.cs`
- New implementation: `integrations/ctrader/calude-edit-v81.cs`
- Phase 6 authority: `CFIPClean81DecisionSnapshot`
- Phase 7 authority: `CFIPClean81EntrySnapshot`
- Phase 8 engine: `CFIPClean81TradePlanBuilder`
- Static tests: `tests/unit/test_ctrader_v81_phase8_risk_targets.py`

## Ownership

Phase 8 is the sole owner of the pre-execution TradePlan's structural stop and target ladder.

It consumes the exact Phase-6 DecisionSnapshot and Phase-7 EntrySnapshot. It does not recompute direction, does not place orders, does not mutate broker state, and does not render chart/UI state.

## Stop model

The stop hierarchy is explicit:

1. Entry structural invalidation.
2. M5 FVG/OB structural boundary.
3. M5 swing structure.
4. H1/H4/D1 swing structure when enabled.
5. Explicit ATR fallback only when structural policy permits it.

The selected structural stop is expanded to the configured minimum ATR risk when necessary. A structural stop beyond the configured maximum ATR risk is rejected when structural protection is mandatory; otherwise the configured ATR fallback is used and marked as fallback provenance.

No fallback is silently presented as structural.

## Target model

One target engine creates the authoritative ladder:

- TP1 minimum RR
- TP2 minimum RR
- TP3 minimum RR
- TP4 minimum RR
- minimum target spacing
- target clearance
- maximum target extension
- opposing-zone obstacle detection
- liquidity targets
- explicit synthetic RR fallback

Higher target stages are published only when their configured RR, spacing, extension and obstacle constraints can be satisfied. TP1 is mandatory for a valid plan.

HTF liquidity is retained as a typed target provenance and can be required by configuration.

## Identity / coherence

Plan identity is derived from the closed MTF reference, direction and entry price. The builder rejects a Phase-7 snapshot whose embedded Decision reference is not the exact Phase-6 snapshot supplied by the host.

## Non-responsibilities

Phase 8 does not:

- size final broker volume
- create broker ExecutionIntent
- place/modify/cancel orders
- confirm ActualFill
- manage live positions
- render chart/UI

Those remain downstream phases.

## Deep-audit corrections

The implementation audit identified and corrected two integrity defects before Phase 8 acceptance:

1. Target candidate selection could choose a liquidity level on the wrong side of the previously selected TP. The engine now requires each subsequent candidate to progress monotonically in the trade direction by at least the configured spacing.
2. Signal identity and Plan identity were initially identical. They are now distinct: `SignalId` identifies the closed-reference directional decision, while `PlanId` identifies the resulting entry/stop plan. This prevents later lifecycle/idempotency domains from conflating a decision with a specific plan.

A valid `TradePlan` now also enforces constructor-level coherence for direction, Entry direction, protective stop and TP1/ordered-ladder requirements.

## Validation

The v81 source-level test suite covers:

- 513/513 parameter parity
- version isolation
- balanced braces
- single TradePlan authority
- exact Decision/Entry snapshot propagation
- structural-stop precedence and explicit fallback
- minimum/maximum risk bounds
- TP1–TP4 target policy
- liquidity and synthetic fallback
- obstacle policy
- target ordering
- broker/UI authority boundary
- downstream Plan persistence

Real cTrader compilation and controlled runtime/scenario validation remain mandatory release gates.
