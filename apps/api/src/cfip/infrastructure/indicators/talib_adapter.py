"""Optional TA-Lib indicator adapter.

TA-Lib is imported lazily so CFIP core remains bootable without the optional
native extension. Market timestamps always come from the caller.
"""
from __future__ import annotations

from cfip.domain.indicator_contracts import IndicatorPoint, IndicatorRequest, IndicatorResult


class TALibUnavailable(RuntimeError):
    """Raised when the optional TA-Lib runtime is not installed."""


def _talib_function(indicator_id: str):
    try:
        import talib
    except ImportError as exc:  # pragma: no cover - depends on environment
        raise TALibUnavailable("TA-Lib is not installed") from exc
    try:
        return getattr(talib, indicator_id.upper()), talib.__version__
    except AttributeError as exc:
        raise ValueError(f"TA-Lib does not expose indicator {indicator_id!r}") from exc


def _int_parameter(value: object, name: str, default: int) -> int:
    if value is None:
        return default
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{name} must be an integer")
    result = int(value)
    if result <= 0:
        raise ValueError(f"{name} must be positive")
    return result


class TALibIndicatorAdapter:
    """Adapter for one-dimensional TA-Lib functions such as EMA, RSI and SMA."""

    id = "talib"
    version = "0.8.0"

    def calculate(self, request: IndicatorRequest) -> IndicatorResult:
        function, library_version = _talib_function(request.indicator_id)
        timestamps = request.aligned_timestamps()
        values = [float(value) for value in request.values]
        kwargs = dict(request.parameters)

        period = _int_parameter(kwargs.pop("timeperiod", kwargs.pop("period", None)), "period", 14)
        supported: dict[str, int] = {"timeperiod": period}
        for name in ("fastperiod", "slowperiod", "signalperiod"):
            if name in kwargs:
                supported[name] = _int_parameter(kwargs.pop(name), name, period)
        if kwargs:
            raise ValueError(f"Unsupported TA-Lib parameters: {sorted(kwargs)}")

        output = function(values, **supported)
        if isinstance(output, tuple):
            raise ValueError(
                f"TA-Lib indicator {request.indicator_id!r} has multiple outputs; "
                "use a dedicated multi-output adapter"
            )

        points = [
            IndicatorPoint(timestamp=timestamps[index], value=float(value))
            for index, value in enumerate(output)
            if value == value
        ]
        return IndicatorResult(
            indicator_id=request.indicator_id,
            version=f"talib-{library_version}",
            points=points,
            metadata={"provider": "TA-Lib", "adapter": self.id},
        )
