import pytest

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


@pytest.mark.asyncio
async def test_qdrant_hybrid_query_uses_dense_sparse_rrf(monkeypatch) -> None:
    from cfip.infrastructure.oss.qdrant_retrieval import QdrantRetrievalAdapter

    class Point:
        id = 7
        score = 0.9
        payload = {"evidence_id": "e1"}

    class Response:
        points = [Point()]

    class Client:
        async def query_points(self, **kwargs):
            assert kwargs["collection_name"] == "evidence"
            assert len(kwargs["prefetch"]) == 2
            assert kwargs["query"].__class__.__name__ == "RrfQuery"
            return Response()

        async def close(self):
            pass

    adapter = QdrantRetrievalAdapter("http://qdrant", "evidence")
    adapter._client = Client()
    result = await adapter.query_hybrid(
        "gold prices",
        dense_embed=lambda _: [0.1, 0.2],
        sparse_embed=lambda _: ([1, 4], [0.8, 0.2]),
    )
    assert result == [{"id": "7", "score": 0.9, "payload": {"evidence_id": "e1"}}]


@pytest.mark.asyncio
async def test_qdrant_hybrid_rejects_invalid_sparse_vectors() -> None:
    from cfip.infrastructure.oss.qdrant_retrieval import QdrantRetrievalAdapter

    adapter = QdrantRetrievalAdapter("http://qdrant", "evidence")
    with pytest.raises(ValueError, match="qdrant_sparse_indices_invalid"):
        await adapter.query_hybrid(
            "gold",
            dense_embed=lambda _: [0.1],
            sparse_embed=lambda _: ([-1], [0.2]),
        )
