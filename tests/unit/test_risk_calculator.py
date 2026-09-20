import pytest

from cfip.domain.risk_contracts import RiskRequest
from cfip.infrastructure.risk.calculator import calculate_risk


def test_risk_calculator_respects_risk_and_leverage_caps() -> None:
    request = RiskRequest(
        symbol="EURUSD",
        side="long",
        equity=10_000,
        risk_fraction=0.01,
        entry_price=1.1,
        stop_price=1.09,
        leverage=20,
        contract_size=100_000,
    )

    result = calculate_risk(request)

    assert result.risk_amount == pytest.approx(100)
    assert result.units_by_risk == pytest.approx(0.1)
    assert result.units_by_leverage == pytest.approx(1.818181818, rel=1e-6)
    assert result.recommended_units == pytest.approx(0.1)
    assert result.margin_required == pytest.approx(550)


def test_risk_request_rejects_invalid_stop_direction() -> None:
    with pytest.raises(ValueError, match="long_stop_must_be_below_entry"):
        RiskRequest(
            symbol="EURUSD",
            side="long",
            equity=10_000,
            risk_fraction=0.01,
            entry_price=1.1,
            stop_price=1.11,
            leverage=20,
        )
