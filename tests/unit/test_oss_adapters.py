"""Contract-level tests for optional OSS adapters without installing heavy vendors."""

from cfip.domain.oss_contracts import (
    ModelRegistryAdapter,
    RetrievalIndexAdapter,
)


def test_mlflow_adapter_implements_model_registry_contract() -> None:
    from cfip.infrastructure.oss.mlflow_registry import MLflowModelRegistryAdapter

    assert isinstance(MLflowModelRegistryAdapter("http://unused"), ModelRegistryAdapter)


def test_qdrant_adapter_implements_retrieval_contract() -> None:
    from cfip.infrastructure.oss.qdrant_retrieval import QdrantRetrievalAdapter

    assert isinstance(
        QdrantRetrievalAdapter("http://unused", "cfip", embed=lambda _: [0.0]),
        RetrievalIndexAdapter,
    )


def test_opentelemetry_adapter_exposes_observability_contract() -> None:
    from cfip.infrastructure.oss.opentelemetry_observability import (
        OpenTelemetryObservabilityAdapter,
    )

    assert hasattr(OpenTelemetryObservabilityAdapter, "span")
    assert hasattr(OpenTelemetryObservabilityAdapter, "metric")
