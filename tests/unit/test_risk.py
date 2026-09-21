from cfip.application.risk import RiskService
from cfip.domain.risk import AccountRiskContext, InstrumentRiskContext, RiskTargetRequest


def _contexts(leverage: float = 100) -> tuple[AccountRiskContext, InstrumentRiskContext]:
    return (
        AccountRiskContext(equity=10_000, risk_fraction=0.01, leverage=leverage, account_currency="USD"),
        InstrumentRiskContext(
            symbol="EUR/USD", base_currency="EUR", quote_currency="USD",
            pip_size=0.0001, tick_size=0.00001, tick_value_per_unit=0.00001,
            min_stop_distance=0.0005, min_quantity=1, max_quantity=100_000, quantity_step=1,
        ),
    )


def _request() -> RiskTargetRequest:
    return RiskTargetRequest(
        direction="long", entry=1.1, atr=0.001, stop_atr_multiplier=1.5,
        target_rr=(1.0, 2.0, 3.0), minimum_rr=1.0,
    )


def test_risk_requires_explicit_context() -> None:
    result = RiskService.plan(_request(), account=None, instrument=None, quote_to_account_rate=None)
    assert result.available is False


def test_risk_calculates_stop_targets_and_quantity() -> None:
    account, instrument = _contexts()
    result = RiskService.plan(_request(), account=account, instrument=instrument, quote_to_account_rate=1.0)
    assert result.available is True
    assert result.stop == 1.0985
    assert result.tp1 == 1.1015
    assert result.tp2 == 1.103
    assert result.tp3 == 1.1045
    assert result.quantity is not None and result.quantity > 0


def test_risk_quantity_is_capped_by_leverage() -> None:
    account, instrument = _contexts(leverage=1)
    result = RiskService.plan(_request(), account=account, instrument=instrument, quote_to_account_rate=1.0)
    assert result.available is True
    assert result.reason == "calculated_leverage_capped"
    assert result.quantity == 9090


def test_risk_rejects_non_finite_conversion_rate() -> None:
    account, instrument = _contexts()
    result = RiskService.plan(_request(), account=account, instrument=instrument, quote_to_account_rate=float("nan"))
    assert result.available is False
    assert result.reason == "quote_to_account_conversion_required"


def test_risk_rejects_non_positive_computed_stop() -> None:
    account, instrument = _contexts()
    request = RiskTargetRequest(
        direction="long", entry=0.0001, atr=1.0, stop_atr_multiplier=2.0,
        target_rr=(1.0, 2.0, 3.0), minimum_rr=1.0,
    )
    result = RiskService.plan(request, account=account, instrument=instrument, quote_to_account_rate=1.0)
    assert result.available is False
    assert result.reason == "stop_price_non_positive"
