"""Deterministic signal lifecycle and outcome analytics services."""
from hashlib import sha256
from math import log

from cfip.domain.outcomes import (
    CalibrationBin,
    CalibrationReport,
    DriftReport,
    OutcomeLabelResult,
    OutcomeObservation,
    SignalEmissionDecision,
    SignalGatePolicy,
    SignalLifecycle,
)


class OutcomeService:
    @staticmethod
    def signal_id(symbol: str, timeframe: str, direction: str, decision_time: int) -> str:
        raw = f"{symbol}|{timeframe}|{direction}|{decision_time}".encode()
        return "sig-" + sha256(raw).hexdigest()[:32]

    @staticmethod
    def emission(
        signal: SignalLifecycle,
        *,
        current_bar_index: int,
        last_emitted_bar_index: int | None,
        policy: SignalGatePolicy,
    ) -> SignalEmissionDecision:
        if current_bar_index < policy.startup_suppression_bars:
            return SignalEmissionDecision(
                eligible=False,
                reason="startup_suppression",
                signal_id=signal.signal_id,
            )
        if (
            last_emitted_bar_index is not None
            and current_bar_index - last_emitted_bar_index
            < max(policy.cooldown_bars, policy.minimum_bars_between_signals)
        ):
            return SignalEmissionDecision(
                eligible=False,
                reason="signal_cooldown",
                signal_id=signal.signal_id,
            )
        if signal.state not in {"accepted", "candidate"}:
            return SignalEmissionDecision(
                eligible=False,
                reason="invalid_signal_state",
                signal_id=signal.signal_id,
            )
        return SignalEmissionDecision(
            eligible=True, reason="eligible", signal_id=signal.signal_id
        )

    @staticmethod
    def label(
        signal: SignalLifecycle,
        *,
        entry: float,
        stop: float | None,
        tp1: float | None,
        tp2: float | None,
        tp3: float | None,
        observations: list[OutcomeObservation],
        evidence_ids: list[str],
    ) -> OutcomeLabelResult:
        if entry <= 0:
            raise ValueError("entry_must_be_positive")
        if signal.direction == "long":
            if stop is not None and stop >= entry:
                raise ValueError("long_stop_must_be_below_entry")
            if any(target is not None and target <= entry for target in (tp1, tp2, tp3)):
                raise ValueError("long_target_must_be_above_entry")
        else:
            if stop is not None and stop <= entry:
                raise ValueError("short_stop_must_be_above_entry")
            if any(target is not None and target >= entry for target in (tp1, tp2, tp3)):
                raise ValueError("short_target_must_be_below_entry")
        observations_after_decision = [
            item for item in observations if item.time >= signal.decision_time
        ]
        if not observations_after_decision:
            if observations:
                raise ValueError("outcome_requires_post_decision_observations")
            return OutcomeLabelResult(
                signal_id=signal.signal_id,
                label="unknown",
                event="expiry",
                decision_time=signal.decision_time,
                evaluation_time=signal.decision_time,
                entry=entry,
                stop=stop,
                tp1=tp1,
                tp2=tp2,
                tp3=tp3,
                evidence_ids=evidence_ids,
            )
        for item in observations_after_decision:
            if signal.direction == "long":
                stop_hit = stop is not None and item.low <= stop
                target_hit = tp1 is not None and item.high >= tp1
            else:
                stop_hit = stop is not None and item.high >= stop
                target_hit = tp1 is not None and item.low <= tp1
            if stop_hit and target_hit:
                return OutcomeLabelResult(
                    signal_id=signal.signal_id,
                    label="unknown",
                    event="ambiguous",
                    decision_time=signal.decision_time,
                    evaluation_time=item.time,
                    entry=entry,
                    stop=stop,
                    tp1=tp1,
                    tp2=tp2,
                    tp3=tp3,
                    evidence_ids=evidence_ids,
                )
            if stop_hit or target_hit:
                return OutcomeLabelResult(
                    signal_id=signal.signal_id,
                    label="loss" if stop_hit else "win",
                    event="stop" if stop_hit else "tp1",
                    decision_time=signal.decision_time,
                    evaluation_time=item.time,
                    entry=entry,
                    stop=stop,
                    tp1=tp1,
                    tp2=tp2,
                    tp3=tp3,
                    evidence_ids=evidence_ids,
                )
        return OutcomeLabelResult(
            signal_id=signal.signal_id,
            label="unknown",
            event="expiry",
            decision_time=signal.decision_time,
            evaluation_time=observations_after_decision[-1].time,
            entry=entry,
            stop=stop,
            tp1=tp1,
            tp2=tp2,
            tp3=tp3,
            evidence_ids=evidence_ids,
        )

    @staticmethod
    def calibration(
        predictions: list[tuple[float, bool]], *, bins: int = 10
    ) -> CalibrationReport:
        if not predictions:
            return CalibrationReport(sample_count=0)
        bins = max(1, min(50, bins))
        sample_count = len(predictions)
        brier = sum((probability - float(outcome)) ** 2 for probability, outcome in predictions) / sample_count
        log_loss = sum(
            -(
                log(max(min(probability, 1 - 1e-15), 1e-15))
                if outcome
                else log(max(1 - min(probability, 1 - 1e-15), 1e-15))
            )
            for probability, outcome in predictions
        ) / sample_count
        calibration_bins: list[CalibrationBin] = []
        ece = 0.0
        for index in range(bins):
            lower = index / bins
            upper = (index + 1) / bins
            members = [
                (probability, outcome)
                for probability, outcome in predictions
                if lower <= probability < upper
                or (index == bins - 1 and probability == upper)
            ]
            if not members:
                continue
            predicted_mean = sum(p for p, _ in members) / len(members)
            observed_rate = sum(y for _, y in members) / len(members)
            ece += len(members) / sample_count * abs(predicted_mean - observed_rate)
            calibration_bins.append(
                CalibrationBin(
                    lower=lower,
                    upper=upper,
                    sample_count=len(members),
                    predicted_mean=predicted_mean,
                    observed_rate=observed_rate,
                )
            )
        return CalibrationReport(
            sample_count=sample_count,
            brier_score=brier,
            log_loss=log_loss,
            expected_calibration_error=ece,
            bins=calibration_bins,
        )

    @staticmethod
    def drift(
        baseline: list[float],
        current: list[float],
        *,
        minimum_sample_count: int = 50,
    ) -> DriftReport:
        if (
            len(baseline) < minimum_sample_count
            or len(current) < minimum_sample_count
        ):
            return DriftReport(
                baseline_count=len(baseline),
                current_count=len(current),
                minimum_sample_count=minimum_sample_count,
                reason="insufficient_samples",
            )
        baseline_mean = sum(baseline) / len(baseline)
        current_mean = sum(current) / len(current)
        baseline_variance = sum(
            (value - baseline_mean) ** 2 for value in baseline
        ) / len(baseline)
        score = abs(current_mean - baseline_mean) / max(
            baseline_variance**0.5, 1e-12
        )
        return DriftReport(
            baseline_count=len(baseline),
            current_count=len(current),
            minimum_sample_count=minimum_sample_count,
            score=score,
            detected=score >= 3.0,
            reason="standardized_mean_shift",
        )
