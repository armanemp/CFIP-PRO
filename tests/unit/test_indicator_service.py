from cfip.application.indicator_service import IndicatorInput, IndicatorService
from cfip.domain.indicator_contracts import IndicatorInstance


def _data(n: int = 80) -> IndicatorInput:
    close = tuple(100 + i * 0.1 + (i % 5) * 0.02 for i in range(n))
    return IndicatorInput(
        time=tuple(1_700_000_000 + i * 60 for i in range(n)),
        open=tuple(x - 0.03 for x in close),
        high=tuple(x + 0.05 for x in close),
        low=tuple(x - 0.05 for x in close),
        close=close,
        volume=tuple(1000 + i for i in range(n)),
    )


def test_indicator_catalog_is_provider_neutral() -> None:
    definitions = IndicatorService().definitions()
    assert {"ema", "sma", "rsi", "atr", "bollinger", "macd"} <= {item.id for item in definitions}


def test_ema_is_normalized() -> None:
    points = IndicatorService().calculate(
        IndicatorInstance(instance_id="i1", definition_id="ema", symbol="EUR/USD", timeframe="1m"),
        _data(),
    )
    assert len(points) == 80
    assert points[-1].values["value"] is not None


def test_unknown_indicator_fails_closed() -> None:
    instance = IndicatorInstance(instance_id="i1", definition_id="unknown", symbol="EUR/USD", timeframe="1m")
    try:
        IndicatorService().calculate(instance, _data())
    except ValueError as exc:
        assert str(exc) == "unsupported_indicator:unknown"
    else:
        raise AssertionError("unsupported indicator must fail closed")
