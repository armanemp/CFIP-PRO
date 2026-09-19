import pytest

from cfip.application.risk_service import RiskService
from cfip.domain.risk_contracts import PositionSizingRequest


def test_position_size_respects_fractional_risk() -> None:
    result = RiskService().size(PositionSizingRequest(
        equity=10_000, risk_fraction=0.01, entry_price=100,
        stop_price=98, contract_multiplier=1, leverage=10, direction="long",
    ))
    assert result.risk_amount == 100
    assert result.stop_distance == 2
    assert result.units == 50


def test_stop_must_differ() -> None:
    with pytest.raises(ValueError, match="stop_must_differ"):
        RiskService().size(PositionSizingRequest(
            equity=10_000, risk_fraction=0.01, entry_price=100,
            stop_price=100, direction="short",
        ))
