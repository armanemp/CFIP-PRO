from cfip.application.alert_engine import AlertEngine
from cfip.domain.alert_contracts import AlertRule

def test_alert_dedupe_is_deterministic() -> None:
    rule = AlertRule(id="r1", name="RSI", symbol="EUR/USD", timeframe="1m", kind="indicator", metric="rsi", operator="above", threshold=70)
    first = AlertEngine().evaluate(rule, 75, 120)
    second = AlertEngine().evaluate(rule, 75, 179)
    assert first.triggered and second.triggered
    assert first.dedupe_key == second.dedupe_key
