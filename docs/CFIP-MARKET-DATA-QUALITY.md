# CFIP-PRO Market Data Quality Policy

## Purpose

CFIP-PRO must never treat sparse or internally discontinuous market data as complete analysis input. This policy is provider-agnostic and sits between normalized candles and deterministic analysis.

## Rules

- The expected interval is derived from the canonical analysis timeframe.
- Duplicate timestamps are collapsed for the quality calculation.
- Gaps larger than the configured contiguous-gap threshold are flagged only when both endpoints are on the same UTC calendar day.
- Cross-day gaps are not automatically classified as missing market bars because forex sessions, weekends, holidays and provider session calendars can legitimately create them.
- Coverage below the configured minimum is degraded.
- Fewer than five closed candles is insufficient.
- Unsupported timeframes are insufficient for continuity gating.
- data_quality is returned in the unified analysis envelope.
- The unified analysis adds a data_quality confluence gate.
- long/short cannot be accepted when quality is degraded or insufficient; the deterministic final recommendation becomes wait.
- No synthetic bars are inserted to repair gaps.

## Current defaults

- contiguous gap threshold: 2 expected intervals
- minimum coverage: 95%

These defaults are policy values, not provider credentials or user account settings. The design leaves room for explicit broker/provider session calendars later, including holiday schedules and venue-specific trading sessions.

## Operational boundary

A provider adapter may report a successful HTTP response while still delivering incomplete market history. Transport success therefore does not imply analysis readiness. The quality report is the explicit evidence boundary.

## Tests

 tests/unit/test_data_quality.py covers cross-day session boundaries, same-day gap detection and insufficient history.
