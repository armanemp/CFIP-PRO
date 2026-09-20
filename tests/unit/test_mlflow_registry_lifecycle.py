import asyncio
import sys
import types

import pytest

from cfip.infrastructure.oss.mlflow_registry import MLflowModelRegistryAdapter


def test_mlflow_alias_lifecycle_uses_registry_client(monkeypatch) -> None:
    calls = []

    class Client:
        def set_registered_model_alias(self, name, alias, version):
            calls.append((name, alias, version))

    module = types.ModuleType("mlflow")
    module.MlflowClient = Client
    monkeypatch.setitem(sys.modules, "mlflow", module)

    adapter = MLflowModelRegistryAdapter()
    asyncio.run(adapter.set_alias("mios-model", "champion", "3"))
    asyncio.run(adapter.rollback_alias("mios-model", "champion", "2"))
    assert calls == [
        ("mios-model", "champion", "3"),
        ("mios-model", "champion", "2"),
    ]


def test_mlflow_alias_rejects_invalid_version() -> None:
    adapter = MLflowModelRegistryAdapter()
    with pytest.raises(ValueError, match="mlflow_model_version_invalid"):
        asyncio.run(adapter.set_alias("mios-model", "champion", "0"))
