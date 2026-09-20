"""Tests for causal signal lifecycle and outcome analytics."""

import pytest

from cfip.application.outcomes import OutcomeService
from cfip.domain.outcomes import OutcomeObservation, SignalGatePolicy, SignalLifecycle


def test_signal_id_is_deterministic() -> None:
    a = OutcomeService.signal_id("EUR/USD", "15m", "long", 1700000000)
    assert a == OutcomeService.signal_id("EUR/USD", "15m", "long", 1700000000)
    assert a != OutcomeService.signal_id("EUR/USD", "15m", "short", 1700000000)


def test_emission_enforces_startup_and_cooldown() -> None:
    signal = SignalLifecycle(
        signal_id="s",
        symbol="EUR/USD",
        timeframe="15m",
        direction="long",
        decision_time=100,
        state="accepted",
    )
    service = OutcomeService()
    assert not service.emission(
        signal,
        current_bar_index=1,
        last_emitted_bar_index=None,
        policy=SignalGatePolicy(startup_suppression_bars=2),
    ).eligible
    assert not service.emission(
        signal,
        current_bar_index=10,
        last_emitted_bar_index=8,
        policy=SignalGatePolicy(cooldown_bars=3, minimum_bars_between_signals=3),
    ).eligible
    assert service.emission(
        signal,
        current_bar_index=12,
        last_emitted_bar_index=8,
        policy=SignalGatePolicy(),
    ).eligible


def test_outcome_never_uses_pre_decision_observations() -> None:
    signal = SignalLifecycle(
        signal_id="s",
        symbol="EUR/USD",
        timeframe="15m",
        direction="long",
        decision_time=100,
        state="emitted",
        emitted_at=100,
    )
    observations = [
        OutcomeObservation(time=99, high=101, low=90, close=95),
        OutcomeObservation(time=101, high=101, low=99, close=100),
    ]
    result = OutcomeService().label(
        signal,
        entry=100,
        stop=98,
        tp1=101,
        tp2=None,
        tp3=None,
        observations=observations,
        evidence_ids=["m1"],
    )
    assert result.evaluation_time == 101
    assert result.label == "win"


def test_outcome_rejects_only_pre_decision_observations() -> None:
    signal = SignalLifecycle(
        signal_id="s",
        symbol="EUR/USD",
        timeframe="15m",
        direction="long",
        decision_time=100,
        state="emitted",
        emitted_at=100,
    )
    try:
        OutcomeService().label(
            signal,
            entry=100,
            stop=98,
            tp1=101,
            tp2=None,
            tp3=None,
            observations=[
                OutcomeObservation(time=99, high=101, low=90, close=95)
            ],
            evidence_ids=["m1"],
        )
    except ValueError as exc:
        assert str(exc) == "outcome_requires_post_decision_observations"
    else:
        raise AssertionError("pre-decision observations must not label an outcome")


def test_calibration_and_drift_have_sample_guards() -> None:
    service = OutcomeService()
    report = service.calibration([(0.9, True), (0.1, False)])
    assert report.sample_count == 2
    assert report.brier_score is not None
    drift = service.drift([0.5] * 49, [0.9] * 50)
    assert not drift.detected
    assert drift.reason == "insufficient_samples"


@pytest.mark.parametrize("probability", [-0.01, 1.01, float("nan"), float("inf")])
def test_calibration_rejects_invalid_probabilities(probability: float) -> None:
    with pytest.raises(ValueError, match="calibration_probability_out_of_range"):
        OutcomeService.calibration([(probability, True)])


def test_drift_rejects_non_finite_values() -> None:
    with pytest.raises(ValueError, match="drift_values_must_be_finite"):
        OutcomeService.drift([0.5] * 49 + [float("nan")], [0.9] * 50)


def test_drift_rejects_invalid_sample_threshold() -> None:
    with pytest.raises(ValueError, match="minimum_sample_count_must_be_positive"):
        OutcomeService.drift([0.5], [0.9], minimum_sample_count=0)


def test_outcome_marks_same_bar_stop_and_target_as_ambiguous() -> None:
    signal = SignalLifecycle(
        signal_id="s",
        symbol="EUR/USD",
        timeframe="15m",
        direction="long",
        decision_time=100,
        state="emitted",
        emitted_at=100,
    )
    result = OutcomeService().label(
        signal,
        entry=100,
        stop=98,
        tp1=102,
        tp2=None,
        tp3=None,
        observations=[OutcomeObservation(time=101, high=103, low=97, close=101)],
        evidence_ids=["m1"],
    )
    assert result.label == "unknown"
    assert result.event == "ambiguous"
