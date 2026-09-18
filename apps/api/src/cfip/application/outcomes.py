"""Deterministic signal lifecycle and outcome analytics services."""
from hashlib import sha256
from math import log
from cfip.domain.outcomes import CalibrationBin, CalibrationReport, DriftReport, SignalEmissionDecision, SignalGatePolicy, OutcomeObservation, SignalLifecycle, OutcomeLabelResult

class OutcomeService:
    @staticmethod
    def signal_id(symbol: str, timeframe: str, direction: str, decision_time: int) -> str:
        return "sig-"+sha256(f"{symbol}|{timeframe}|{direction}|{decision_time}".encode()).hexdigest()[:32]

    @staticmethod
    def emission(signal: SignalLifecycle, *, current_bar_index: int, last_emitted_bar_index: int | None, policy: SignalGatePolicy) -> SignalEmissionDecision:
        if current_bar_index < policy.startup_suppression_bars:
            return SignalEmissionDecision(eligible=False, reason="startup_suppression", signal_id=signal.signal_id)
        if last_emitted_bar_index is not None and current_bar_index-last_emitted_bar_index < max(policy.cooldown_bars, policy.minimum_bars_between_signals):
            return SignalEmissionDecision(eligible=False, reason="signal_cooldown", signal_id=signal.signal_id)
        if signal.state not in {"accepted","candidate"}:
            return SignalEmissionDecision(eligible=False, reason="invalid_signal_state", signal_id=signal.signal_id)
        return SignalEmissionDecision(eligible=True, reason="eligible", signal_id=signal.signal_id)

    @staticmethod
    def label(signal: SignalLifecycle, *, entry: float, stop: float | None, tp1: float | None, tp2: float | None, tp3: float | None, observations: list[OutcomeObservation], evidence_ids: list[str]) -> OutcomeLabelResult:
        obs=[item for item in observations if item.time >= signal.decision_time]
        if not obs:
            if observations: raise ValueError("outcome_requires_post_decision_observations")
            return OutcomeLabelResult(signal_id=signal.signal_id,label="unknown",event="expiry",decision_time=signal.decision_time,evaluation_time=signal.decision_time,entry=entry,stop=stop,tp1=tp1,tp2=tp2,tp3=tp3,evidence_ids=evidence_ids)
        for item in obs:
            if signal.direction=="long":
                if stop is not None and item.low <= stop:
                    return OutcomeLabelResult(signal_id=signal.signal_id,label="loss",event="stop",decision_time=signal.decision_time,evaluation_time=item.time,entry=entry,stop=stop,tp1=tp1,tp2=tp2,tp3=tp3,evidence_ids=evidence_ids)
                if tp1 is not None and item.high >= tp1:
                    return OutcomeLabelResult(signal_id=signal.signal_id,label="win",event="tp1",decision_time=signal.decision_time,evaluation_time=item.time,entry=entry,stop=stop,tp1=tp1,tp2=tp2,tp3=tp3,evidence_ids=evidence_ids)
            else:
                if stop is not None and item.high >= stop:
                    return OutcomeLabelResult(signal_id=signal.signal_id,label="loss",event="stop",decision_time=signal.decision_time,evaluation_time=item.time,entry=entry,stop=stop,tp1=tp1,tp2=tp2,tp3=tp3,evidence_ids=evidence_ids)
                if tp1 is not None and item.low <= tp1:
                    return OutcomeLabelResult(signal_id=signal.signal_id,label="win",event="tp1",decision_time=signal.decision_time,evaluation_time=item.time,entry=entry,stop=stop,tp1=tp1,tp2=tp2,tp3=tp3,evidence_ids=evidence_ids)
        return OutcomeLabelResult(signal_id=signal.signal_id,label="unknown",event="expiry",decision_time=signal.decision_time,evaluation_time=obs[-1].time,entry=entry,stop=stop,tp1=tp1,tp2=tp2,tp3=tp3,evidence_ids=evidence_ids)

    @staticmethod
    def calibration(predictions: list[tuple[float,bool]], *, bins: int=10) -> CalibrationReport:
        if not predictions: return CalibrationReport(sample_count=0)
        bins=max(1,min(50,bins)); n=len(predictions)
        brier=sum((p-float(y))**2 for p,y in predictions)/n
        ll=sum(-(log(max(min(p,1-1e-15),1e-15)) if y else log(max(1-min(p,1-1e-15),1e-15))) for p,y in predictions)/n
        result=[]; ece=0.0
        for i in range(bins):
            lo=i/bins; hi=(i+1)/bins
            members=[(p,y) for p,y in predictions if lo<=p<hi or (i==bins-1 and p==hi)]
            if not members: continue
            mean=sum(p for p,_ in members)/len(members); rate=sum(y for _,y in members)/len(members)
            ece += len(members)/n*abs(mean-rate)
            result.append(CalibrationBin(lower=lo,upper=hi,sample_count=len(members),predicted_mean=mean,observed_rate=rate))
        return CalibrationReport(sample_count=n,brier_score=brier,log_loss=ll,expected_calibration_error=ece,bins=result)

    @staticmethod
    def drift(baseline: list[float], current: list[float], *, minimum_sample_count:int=50) -> DriftReport:
        if len(baseline)<minimum_sample_count or len(current)<minimum_sample_count:
            return DriftReport(baseline_count=len(baseline),current_count=len(current),minimum_sample_count=minimum_sample_count,reason="insufficient_samples")
        bm=sum(baseline)/len(baseline); cm=sum(current)/len(current)
        bv=sum((x-bm)**2 for x in baseline)/len(baseline)
        score=abs(cm-bm)/(max(bv**0.5,1e-12))
        return DriftReport(baseline_count=len(baseline),current_count=len(current),minimum_sample_count=minimum_sample_count,score=score,detected=score>=3.0,reason="standardized_mean_shift")
