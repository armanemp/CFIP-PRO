import asyncio
import sys
import types

from cfip.infrastructure.oss.evidently_evaluation import EvidentlyEvaluationAdapter


def test_evidently_adapter_uses_optional_dependency(monkeypatch) -> None:
    calls: list[tuple[object, object]] = []

    class Snapshot:
        def dict(self):
            return {"metrics": [{"id": "drift"}]}

    class Report:
        def __init__(self, metrics, include_tests):
            assert metrics
            assert include_tests is True

        def run(self, current, reference):
            calls.append((current, reference))
            return Snapshot()

    evidently = types.ModuleType("evidently")
    evidently.Report = Report
    presets = types.ModuleType("evidently.presets")
    presets.DataDriftPreset = lambda: object()
    monkeypatch.setitem(sys.modules, "evidently", evidently)
    monkeypatch.setitem(sys.modules, "evidently.presets", presets)

    result = asyncio.run(
        EvidentlyEvaluationAdapter().evaluate_drift([{"x": 1}], [{"x": 0}])
    )
    assert result == {"metrics": [{"id": "drift"}]}
    assert len(calls) == 1


def test_evidently_adapter_reports_missing_optional_dependency(monkeypatch) -> None:
    monkeypatch.delitem(sys.modules, "evidently", raising=False)
    monkeypatch.delitem(sys.modules, "evidently.presets", raising=False)

    real_import = __import__

    def blocked(name, *args, **kwargs):
        if name == "evidently":
            raise ImportError("blocked")
        return real_import(name, *args, **kwargs)

    monkeypatch.setattr("builtins.__import__", blocked)

    try:
        asyncio.run(EvidentlyEvaluationAdapter().evaluate_drift([], []))
    except RuntimeError as exc:
        assert "evidently_optional_dependency_missing" in str(exc)
    else:
        raise AssertionError("expected optional dependency failure")
