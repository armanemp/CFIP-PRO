# CFIP-PRO cTrader v84 — Phase 11 Position Lifecycle

Date: 2026-09-27

## Objective

Make broker-managed live Positions a first-class lifecycle with broker reality as the authoritative source.

cTrader exposes Opened, Modified and Closed position events, and the Position API exposes identity, entry, SL/TP, label, comment and close information. citeturn2view0turn2view1turn1search7

## Implemented

### Position lifecycle record

`CFIPClean84PositionRecord` tracks:

- broker Position identity
- strategy / SignalId / PlanId
- direction
- original and actual entry price
- expected protection
- current volume
- adoption/close timestamps
- explicit lifecycle state

### Broker event ownership

The host observes:

- `Positions.Opened`
- `Positions.Modified`
- `Positions.Closed`

Event handlers only update lifecycle state and reconciliation data. Broker mutation remains inside the broker gateway. citeturn1search5turn1search3

### Protection recovery

A managed Position with missing or drifted protection becomes an explicit recovery state. Recovery is executed through the existing broker gateway, which already centralizes Position modification.

### Restart recovery

Existing managed Positions are adopted at startup. Reconciliation uses broker Position snapshots as authoritative:

- a Position still present at broker remains owned internally;
- a missing broker Position is reconciled as no longer live;
- a managed Position without recoverable PlanId is marked `Orphan`, not automatically closed.

### Pending → Position handoff

When a PendingOrder Filled event supplies both the filled PendingOrder and resulting Position, the Position lifecycle receives the expected pending protection and actual broker entry. cTrader documents this event contract directly. citeturn0search5

## Invariants

- Internal live ownership is never cleared while the managed broker Position still exists.
- Position events are not independent trading authorities.
- Protection recovery is broker-gateway-owned.
- Orphan positions are detected explicitly and are not silently closed.
- Actual broker entry is authoritative after fill.
- Restart does not require the original in-memory event sequence.

## Validation status

Static tests cover:

- parameter parity with v83
- version isolation
- source balance
- position event wiring
- startup adoption
- restart reconciliation
- orphan detection
- protection drift
- broker mutation boundary
- filled pending → Position handoff

No real cTrader compile/runtime result is claimed yet.

## Remaining Phase 11 work

- close request/result retry semantics
- deterministic position close confirmation
- partial-fill / multi-position ownership
- explicit protection mutation retry/backoff
- broker disconnect behavior
- runtime scenario validation
- orphan remediation policy after governance rules are finalized
