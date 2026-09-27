# CFIP-PRO cTrader v87 — Phase 12 Live Position Management

Date: 2026-09-27

## Scope

v87 adds the first-class Live Position Management layer on top of the authoritative Position lifecycle.

## Implemented

- A single Live Position manager produces management actions for active CFIP positions.
- The management path is plan-isolated: the current analytical plan is only applied to a Position when its broker comment identifies the same PlanId.
- SL management combines break-even, spread-aware risk-free locking, structural repricing and adaptive trailing while never proposing an SL relaxation.
- Target management advances only unhit targets and retains the broker target in the safer direction.
- Partial TP uses broker volume constraints and executes only through the broker gateway.
- Reversal protection, fast reversal, structural invalidation, profit exhaustion and end-of-day auto-close are evaluated in one forced-exit domain.
- Protection updates are proposal/confirmation based; target-stage state is advanced only from broker observation.
- Structural target repricing honors StructuralTargetUpdatesOnly, closed-reference cadence and TargetUpdateStepAtr.
- Pending broker mutations are not duplicated while awaiting broker-state confirmation.
- No manual trade-entry controls are introduced.

## Safety invariants

1. A protection request is not a protection confirmation.
2. A close request is not a close confirmation.
3. A target-stage proposal is not a target-stage transition until broker state confirms it.
4. A live position is never managed from a different current Plan.
5. Broker mutations remain gateway-owned.
6. Broker disconnect prevents live management and mutation.
7. Partial close cannot request the full remaining position.

## Remaining Phase 12 validation

Implementation is complete at the source/static-contract level. Controlled cTrader compilation and runtime scenarios remain mandatory, including:
- BE/trailing with broker stop-distance limits
- TP stage advancement and delayed modification events
- partial-close accepted/rejected/partially-executed scenarios
- reversal/invalidation/exhaustion precedence
- position restart/re-adoption
- broker reconnect during management