"""Optional MLflow Model Registry adapter.

MLflow stays outside CFIP's domain/application layers. The import is lazy so
native development does not require MLflow until the adapter is configured.
"""
from __future__ import annotations

import asyncio
from typing import Any


class MLflowModelRegistryAdapter:
    provider_id = "mlflow"

    def __init__(self, tracking_uri: str | None = None) -> None:
        self._tracking_uri = tracking_uri

    def _client(self) -> Any:
        try:
            import mlflow
        except ImportError as exc:
            raise RuntimeError("oss_dependency_missing:mlflow") from exc
        if self._tracking_uri:
            mlflow.set_tracking_uri(self._tracking_uri)
        return mlflow

    async def register(self, name: str, artifact_uri: str, *, metadata: dict[str, str]) -> str:
        if not name.strip():
            raise ValueError("mlflow_model_name_required")
        if not artifact_uri.strip():
            raise ValueError("mlflow_artifact_uri_required")

        def _register() -> str:
            mlflow = self._client()
            result = mlflow.register_model(artifact_uri, name)
            for key, value in metadata.items():
                mlflow.set_model_version_tag(name, result.version, key, value)
            return str(result.version)

        return await asyncio.to_thread(_register)

    async def resolve(self, name: str, alias: str = "champion") -> str | None:
        if not name.strip():
            raise ValueError("mlflow_model_name_required")
        if not alias.strip():
            raise ValueError("mlflow_alias_required")

        def _resolve() -> str | None:
            mlflow = self._client()
            client = mlflow.MlflowClient()
            try:
                model = client.get_model_version_by_alias(name, alias)
            except Exception as exc:
                exception_types = tuple(
                    item
                    for item in (
                        getattr(getattr(mlflow, "exceptions", None), "RestException", None),
                        getattr(getattr(mlflow, "exceptions", None), "MlflowException", None),
                    )
                    if isinstance(item, type)
                )
                if exception_types and isinstance(exc, exception_types):
                    return None
                raise
            return str(model.source)

        return await asyncio.to_thread(_resolve)
