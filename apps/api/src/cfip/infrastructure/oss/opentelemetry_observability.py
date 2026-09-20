"""Optional OpenTelemetry adapter for vendor-neutral traces and metrics."""
from __future__ import annotations

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

    def span(self, name: str, attributes: dict[str, Any] | None = None) -> Any:
        return self._tracer.start_as_current_span(name, attributes=attributes or {})

    def metric(self, name: str, value: float, attributes: dict[str, Any] | None = None) -> None:
        counter = self._meter.create_counter(name)
        counter.add(value, attributes or {})
