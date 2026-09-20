from fastapi.testclient import TestClient

from cfip.main import app


def test_risk_plan_endpoint_requires_explicit_context() -> None:
    client = TestClient(app)
    response = client.post(
        "/api/risk/plan",
        json={
            "target": {
                "direction": "long",
                "entry": 1.1,
                "atr": 0.001,
                "stop_atr_multiplier": 1.5,
                "target_rr": [1.0, 2.0, 3.0],
                "minimum_rr": 1.0,
            }
        },
    )
    assert response.status_code == 200
    payload = response.json()
    assert payload["available"] is False
    assert payload["reason"] == "account_or_instrument_context_required"


def test_risk_plan_endpoint_returns_broker_aware_plan() -> None:
    client = TestClient(app)
    response = client.post(
        "/api/risk/plan",
        json={
            "target": {
                "direction": "long",
                "entry": 1.1,
                "atr": 0.001,
                "stop_atr_multiplier": 1.5,
                "target_rr": [1.0, 2.0, 3.0],
                "minimum_rr": 1.0,
            },
            "account": {
                "equity": 10000,
                "risk_fraction": 0.01,
                "leverage": 100,
                "account_currency": "USD",
            },
            "instrument": {
                "symbol": "EUR/USD",
                "base_currency": "EUR",
                "quote_currency": "USD",
                "pip_size": 0.0001,
                "tick_size": 0.00001,
                "tick_value_per_unit": 0.00001,
                "min_stop_distance": 0.0005,
                "min_quantity": 1,
                "max_quantity": 100000,
                "quantity_step": 1,
            },
            "quote_to_account_rate": 1.0,
        },
    )
    assert response.status_code == 200
    payload = response.json()
    assert payload["available"] is True
    assert payload["quantity"] == 66666
    assert payload["stop"] == 1.0985
    assert payload["tp3"] == 1.1045
