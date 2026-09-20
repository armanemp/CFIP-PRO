import sys
import types

from cfip.infrastructure.oss.opentelemetry_observability import OpenTelemetryObservabilityAdapter


def test_otel_propagation_delegates_to_vendor_api(monkeypatch) -> None:
    captured = {}

    class Propagate:
        @staticmethod
        def inject(carrier):
            carrier["traceparent"] = "00-test"

        @staticmethod
        def extract(carrier):
            captured["carrier"] = dict(carrier)
            return "context"

    class Tracer:
        def start_as_current_span(self, name, attributes):
            return (name, attributes)

    class Meter:
        def create_counter(self, name):
            return type("Counter", (), {"add": lambda self, value, attributes: None})()

    otel = types.ModuleType("opentelemetry")
    otel.propagate = Propagate
    otel.trace = types.SimpleNamespace(get_tracer=lambda _: Tracer())
    otel.metrics = types.SimpleNamespace(get_meter=lambda _: Meter())
    monkeypatch.setitem(sys.modules, "opentelemetry", otel)

    adapter = OpenTelemetryObservabilityAdapter()
    carrier = {}
    adapter.inject(carrier)
    assert carrier["traceparent"] == "00-test"
    assert adapter.extract(carrier) == "context"
    assert captured["carrier"] == carrier
