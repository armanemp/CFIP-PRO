"""Optional OpenTelemetry adapter for vendor-neutral traces and metrics."""
from __future__ import annotations

from threading import Lock
from typing import Any


class OpenTelemetryObservabilityAdapter:
    provider_id = "opentelemetry"

    def __init__(self, service_name: str = "cfip-pro") -> None:
        try:
            from opentelemetry import metrics, trace
        except ImportError as exc:
            raise RuntimeError("oss_dependency_missing:opentelemetry") from exc
        self._tracer = trace.get_tracer(service_name)
        self._meter = metrics.get_meter(service_name)
        self._counters: dict[str, Any] = {}
        self._counter_lock = Lock()

    def span(self, name: str, attributes: dict[str, Any] | None = None) -> Any:
        return self._tracer.start_as_current_span(name, attributes=attributes or {})

    def metric(self, name: str, value: float, attributes: dict[str, Any] | None = None) -> None:
        if not isinstance(value, (int, float)):
            raise TypeError("metric_value_must_be_numeric")
        with self._counter_lock:
            counter = self._counters.get(name)
            if counter is None:
                counter = self._meter.create_counter(name)
                self._counters[name] = counter
        counter.add(float(value), attributes or {})
