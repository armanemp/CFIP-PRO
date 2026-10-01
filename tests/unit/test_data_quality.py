from cfip.domain.data_quality import DataQualityPolicy, assess_data_quality


def test_quality_ignores_cross_day_session_boundary() -> None:
    policy = DataQualityPolicy(expected_interval_seconds=3600)
    times = [
        1726786800,  # Thursday 23:00 UTC
        1726794000,  # Friday 01:00 UTC
    ]
    report = assess_data_quality(times, policy)
    assert report.contiguous_gap_count == 0


def test_quality_detects_same_day_contiguous_gap() -> None:
    policy = DataQualityPolicy(expected_interval_seconds=60, max_contiguous_gap_intervals=2)
    times = [1726740000, 1726740060, 1726740360, 1726740420, 1726740480]
    report = assess_data_quality(times, policy)
    assert report.contiguous_gap_count == 1
    assert report.status == "degraded"
    assert report.largest_contiguous_gap_seconds == 300


def test_quality_reports_insufficient_history() -> None:
    policy = DataQualityPolicy(expected_interval_seconds=60)
    report = assess_data_quality([1726740000, 1726740060], policy)
    assert report.status == "insufficient"
    assert "too_few_bars" in report.reasons
