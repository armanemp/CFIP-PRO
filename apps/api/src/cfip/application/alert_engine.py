"""Provider-neutral alert evaluation with deterministic dedupe keys."""
from __future__ import annotations
from cfip.domain.alert_contracts import AlertEvaluation, AlertRule

class AlertEngine:
    def evaluate(self, rule: AlertRule, value: float | str, evaluated_at: int) -> AlertEvaluation:
        triggered = False
        if rule.operator == "above": triggered = float(value) > float(rule.threshold)
        elif rule.operator == "below": triggered = float(value) < float(rule.threshold)
        elif rule.operator == "equals": triggered = str(value) == str(rule.threshold)
        elif rule.operator == "crosses-above": triggered = float(value) > float(rule.threshold)
        elif rule.operator == "crosses-below": triggered = float(value) < float(rule.threshold)
        return AlertEvaluation(
            rule_id=rule.id, triggered=triggered, value=value, evaluated_at=evaluated_at,
            message=f"{rule.name}: {value}" if triggered else "",
            dedupe_key=f"{rule.id}:{evaluated_at // max(rule.cooldown_seconds, 1)}",
        )
