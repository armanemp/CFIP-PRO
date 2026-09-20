"""Deterministic market-data quality and continuity policy.

The policy is deliberately provider-agnostic. It distinguishes missing bars inside a
continuous UTC trading day from expected session/weekend boundaries, so sparse data
cannot silently masquerade as complete MTF input.
"""

from __future__ import annotations

from datetime import UTC, datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

QualityStatus = Literal["ok", "degraded", "insufficient"]


class DataQualityPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")

    expected_interval_seconds: int = Field(gt=0)
    max_contiguous_gap_intervals: int = Field(default=2, ge=1, le=12)
    minimum_coverage: float = Field(default=0.95, gt=0, le=1)
    calendar_month: bool = False


class DataQualityReport(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: QualityStatus
    expected_interval_seconds: int
    candle_count: int = Field(ge=0)
    contiguous_gap_count: int = Field(ge=0)
    largest_contiguous_gap_seconds: int = Field(ge=0)
    coverage_ratio: float = Field(ge=0, le=1)
    first_bar_time: int | None = None
    last_bar_time: int | None = None
    reasons: list[str] = Field(default_factory=list)


def _is_same_utc_day(left: int, right: int) -> bool:
    left_day = datetime.fromtimestamp(left, tz=UTC).date()
    right_day = datetime.fromtimestamp(right, tz=UTC).date()
    return left_day == right_day


def _month_index(timestamp: int) -> int:
    value = datetime.fromtimestamp(timestamp, tz=UTC)
    return value.year * 12 + value.month


def _calendar_month_delta(left: int, right: int) -> int:
    return max(1, _month_index(right) - _month_index(left))


def assess_data_quality(times: list[int], policy: DataQualityPolicy) -> DataQualityReport:
    ordered = sorted(set(int(value) for value in times if int(value) > 0))
    if not ordered:
        return DataQualityReport(
            status="insufficient",
            expected_interval_seconds=policy.expected_interval_seconds,
            candle_count=0,
            contiguous_gap_count=0,
            largest_contiguous_gap_seconds=0,
            coverage_ratio=0.0,
            reasons=["no_market_bars"],
        )

    expected = policy.expected_interval_seconds
    threshold = expected * policy.max_contiguous_gap_intervals
    gaps: list[int] = []
    missing_intervals = 0
    for previous, current in zip(ordered, ordered[1:], strict=False):
        delta = current - previous
        if policy.calendar_month:
            interval_count = _calendar_month_delta(previous, current)
            expected_delta = max(expected, delta if interval_count == 1 else expected)
            if interval_count > policy.max_contiguous_gap_intervals and delta > expected_delta:
                gaps.append(delta)
                missing_intervals += interval_count - 1
        elif delta > threshold and _is_same_utc_day(previous, current):
            gaps.append(delta)
            missing_intervals += max(0, (delta // expected) - 1)

    covered_intervals = max(0, len(ordered) - 1)
    expected_intervals = covered_intervals + missing_intervals
    coverage = (
        covered_intervals / expected_intervals
        if expected_intervals > 0
        else 1.0
    )
    reasons: list[str] = []
    if gaps:
        reasons.append(f"{len(gaps)} unexpected contiguous data gap(s)")
    if coverage < policy.minimum_coverage:
        reasons.append(
            f"coverage {coverage:.3f} below minimum {policy.minimum_coverage:.3f}"
        )

    if len(ordered) < 5:
        status: QualityStatus = "insufficient"
        reasons.append("too_few_bars")
    elif coverage < policy.minimum_coverage:
        status = "degraded"
    else:
        status = "ok"

    return DataQualityReport(
        status=status,
        expected_interval_seconds=expected,
        candle_count=len(ordered),
        contiguous_gap_count=len(gaps),
        largest_contiguous_gap_seconds=max(gaps, default=0),
        coverage_ratio=round(coverage, 6),
        first_bar_time=ordered[0],
        last_bar_time=ordered[-1],
        reasons=reasons,
    )
