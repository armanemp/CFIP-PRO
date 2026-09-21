"""Vendor-neutral contracts for the MIOS intelligence runtime.

The intelligence layer is deliberately independent from PydanticAI, LangGraph,
Haystack, DSPy, model vendors, vector stores, and research providers. OSS
implementations must adapt to these contracts instead of leaking vendor objects
into the domain.
"""

from __future__ import annotations

from collections.abc import Mapping, Sequence
from typing import Any, Protocol, runtime_checkable

from pydantic import BaseModel, ConfigDict, Field


class IntelligenceEvidence(BaseModel):
    model_config = ConfigDict(extra="forbid")

    evidence_id: str = Field(min_length=1, max_length=160)
    source: str = Field(min_length=1, max_length=500)
    kind: str = Field(min_length=1, max_length=80)
    content_digest: str = Field(min_length=1, max_length=128)
    observed_at: int = Field(gt=0)
    metadata: dict[str, str] = Field(default_factory=dict)


class IntelligenceExecutionContext(BaseModel):
    model_config = ConfigDict(extra="forbid")

    request_id: str = Field(min_length=1, max_length=128)
    trace_id: str | None = Field(default=None, max_length=128)
    actor: str = Field(min_length=1, max_length=128)
    purpose: str = Field(min_length=1, max_length=200)
    evidence: tuple[IntelligenceEvidence, ...] = ()
    dry_run: bool = True
    approval_required: bool = True


class AgentSpec(BaseModel):
    model_config = ConfigDict(extra="forbid")

    agent_id: str = Field(min_length=1, max_length=128)
    version: str = Field(min_length=1, max_length=64)
    capabilities: tuple[str, ...] = ()
    model_id: str = Field(min_length=1, max_length=160)
    tool_ids: tuple[str, ...] = ()
    memory_id: str | None = Field(default=None, max_length=160)
    research_id: str | None = Field(default=None, max_length=160)


class AgentRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    spec: AgentSpec
    prompt: str = Field(min_length=1, max_length=20_000)
    context: IntelligenceExecutionContext
    input: Mapping[str, Any] = Field(default_factory=dict)


class AgentResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    output: Any
    evidence: tuple[IntelligenceEvidence, ...] = ()
    tool_calls: tuple[str, ...] = ()
    model_id: str = Field(min_length=1, max_length=160)
    latency_ms: int = Field(ge=0)
    usage: dict[str, float] = Field(default_factory=dict)
    confidence: float | None = Field(default=None, ge=0.0, le=1.0)
    trace_id: str | None = Field(default=None, max_length=128)


class ToolSpec(BaseModel):
    model_config = ConfigDict(extra="forbid")

    tool_id: str = Field(min_length=1, max_length=128)
    version: str = Field(min_length=1, max_length=64)
    description: str = Field(min_length=1, max_length=2_000)
    side_effects: bool = False
    approval_required: bool = True


class ToolRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    spec: ToolSpec
    arguments: Mapping[str, Any] = Field(default_factory=dict)
    context: IntelligenceExecutionContext


class ToolResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    output: Any
    evidence: tuple[IntelligenceEvidence, ...] = ()
    changed_state: bool = False


class ModelSpec(BaseModel):
    model_config = ConfigDict(extra="forbid")

    model_id: str = Field(min_length=1, max_length=160)
    version: str = Field(min_length=1, max_length=128)
    provider: str = Field(min_length=1, max_length=128)
    capabilities: tuple[str, ...] = ()
    deterministic: bool = False


class MemoryRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")

    record_id: str = Field(min_length=1, max_length=160)
    namespace: str = Field(min_length=1, max_length=160)
    content: str = Field(min_length=1, max_length=20_000)
    content_digest: str = Field(min_length=1, max_length=128)
    evidence_ids: tuple[str, ...] = ()
    created_at: int = Field(gt=0)
    expires_at: int | None = Field(default=None, gt=0)


class ResearchRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    query: str = Field(min_length=1, max_length=10_000)
    sources: tuple[str, ...] = ()
    limit: int = Field(default=10, ge=1, le=100)
    context: IntelligenceExecutionContext


@runtime_checkable
class AgentRuntime(Protocol):
    provider_id: str

    async def run(self, request: AgentRequest) -> AgentResponse: ...


@runtime_checkable
class ToolRuntime(Protocol):
    provider_id: str

    async def execute(self, request: ToolRequest) -> ToolResponse: ...


@runtime_checkable
class ModelRuntime(Protocol):
    provider_id: str

    async def generate(
        self, model: ModelSpec, prompt: str, *, context: IntelligenceExecutionContext
    ) -> Any: ...


@runtime_checkable
class MemoryRuntime(Protocol):
    provider_id: str

    async def put(self, record: MemoryRecord) -> None: ...

    async def search(self, namespace: str, query: str, *, limit: int = 10) -> Sequence[MemoryRecord]: ...


@runtime_checkable
class ResearchRuntime(Protocol):
    provider_id: str

    async def search(self, request: ResearchRequest) -> Sequence[IntelligenceEvidence]: ...
