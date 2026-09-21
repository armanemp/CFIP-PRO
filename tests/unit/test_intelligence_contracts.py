"""Tests for the vendor-neutral MIOS intelligence contracts and adapter."""

from types import SimpleNamespace

import pytest

from cfip.domain.intelligence_contracts import (
    AgentRequest,
    AgentRuntime,
    AgentSpec,
    IntelligenceExecutionContext,
    IntelligenceEvidence,
    MemoryRecord,
    ModelSpec,
    ResearchRequest,
    ResearchRuntime,
    ToolRequest,
    ToolRuntime,
    ToolSpec,
)
from cfip.infrastructure.oss.pydantic_ai_agent import PydanticAIAgentAdapter


def _context() -> IntelligenceExecutionContext:
    return IntelligenceExecutionContext(
        request_id="req-1",
        actor="test",
        purpose="market-analysis",
        evidence=(
            IntelligenceEvidence(
                evidence_id="ev-1",
                source="test",
                kind="market",
                content_digest="sha256:test",
                observed_at=1,
            ),
        ),
    )


def test_contracts_are_runtime_checkable() -> None:
    class FakeAgent:
        provider_id = "fake"

        async def run(self, request):
            raise NotImplementedError

    class FakeTool:
        provider_id = "fake"

        async def execute(self, request):
            raise NotImplementedError

    class FakeResearch:
        provider_id = "fake"

        async def search(self, request):
            raise NotImplementedError

    assert isinstance(FakeAgent(), AgentRuntime)
    assert isinstance(FakeTool(), ToolRuntime)
    assert isinstance(FakeResearch(), ResearchRuntime)


def test_agent_request_is_evidence_and_approval_aware() -> None:
    request = AgentRequest(
        spec=AgentSpec(
            agent_id="market-analyst",
            version="1",
            model_id="model-1",
            tool_ids=("market-data",),
        ),
        prompt="Analyse EURUSD",
        context=_context(),
    )
    assert request.context.approval_required is True
    assert request.context.evidence[0].evidence_id == "ev-1"


def test_memory_and_research_requests_have_bounded_inputs() -> None:
    record = MemoryRecord(
        record_id="m-1",
        namespace="research",
        content="fact",
        content_digest="sha256:fact",
        created_at=1,
    )
    request = ResearchRequest(query="EURUSD", context=_context(), limit=10)
    assert record.namespace == "research"
    assert request.limit == 10


@pytest.mark.asyncio
async def test_pydantic_ai_adapter_normalizes_result() -> None:
    class FakeAgent:
        async def run(self, prompt, *, deps):
            assert prompt == "Analyse EURUSD"
            assert deps == {}
            return SimpleNamespace(output={"decision": "observe"})

    adapter = PydanticAIAgentAdapter(FakeAgent(), model_id="test-model")
    response = await adapter.run(
        AgentRequest(
            spec=AgentSpec(
                agent_id="market-analyst",
                version="1",
                model_id="test-model",
            ),
            prompt="Analyse EURUSD",
            context=_context(),
        )
    )
    assert response.output == {"decision": "observe"}
    assert response.model_id == "test-model"
    assert response.evidence[0].evidence_id == "ev-1"


def test_pydantic_ai_adapter_rejects_missing_run() -> None:
    adapter = PydanticAIAgentAdapter(object(), model_id="test-model")
    with pytest.raises(TypeError, match="run_method_required"):
        # The async method is not reached; validation is intentionally local.
        import asyncio

        asyncio.run(
            adapter.run(
                AgentRequest(
                    spec=AgentSpec(
                        agent_id="market-analyst",
                        version="1",
                        model_id="test-model",
                    ),
                    prompt="test",
                    context=_context(),
                )
            )
        )


def test_model_spec_requires_provider_identity() -> None:
    spec = ModelSpec(model_id="m", version="1", provider="p")
    assert spec.provider == "p"


def test_tool_request_defaults_to_empty_arguments() -> None:
    request = ToolRequest(
        spec=ToolSpec(
            tool_id="market-data",
            version="1",
            description="Read normalized market data",
        ),
        context=_context(),
    )
    assert request.arguments == {}
