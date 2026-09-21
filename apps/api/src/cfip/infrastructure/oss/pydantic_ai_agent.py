"""PydanticAI adapter for the CFIP-owned agent runtime contract.

PydanticAI remains an implementation detail. The adapter accepts an injected
Agent-like object so the domain does not import or depend on PydanticAI types.
"""

from __future__ import annotations

from time import perf_counter
from typing import Any

from cfip.domain.intelligence_contracts import AgentRequest, AgentResponse, AgentRuntime


class PydanticAIAgentAdapter:
    provider_id = "pydantic-ai"

    def __init__(self, agent: Any, *, model_id: str) -> None:
        if agent is None:
            raise ValueError("pydantic_ai_agent_required")
        if not model_id.strip():
            raise ValueError("pydantic_ai_model_id_required")
        self._agent = agent
        self._model_id = model_id

    async def run(self, request: AgentRequest) -> AgentResponse:
        if not isinstance(self, AgentRuntime):
            raise TypeError("adapter_contract_violation:pydantic-ai")

        started = perf_counter()
        run = getattr(self._agent, "run", None)
        if not callable(run):
            raise TypeError("pydantic_ai_run_method_required")

        result = await run(
            request.prompt,
            deps=dict(request.input),
        )
        output = getattr(result, "output", result)
        latency_ms = max(0, round((perf_counter() - started) * 1000))

        return AgentResponse(
            output=output,
            evidence=request.context.evidence,
            model_id=self._model_id,
            latency_ms=latency_ms,
            trace_id=request.context.trace_id,
        )
