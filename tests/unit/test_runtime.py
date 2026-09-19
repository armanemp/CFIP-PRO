"""Tests for concurrent platform runtime orchestration."""

import asyncio

import pytest

from cfip.domain.runtime_contracts import ComponentState, RuntimeComponent
from cfip.infrastructure.runtime import PlatformRuntime


@pytest.mark.asyncio
async def test_runtime_starts_registered_components_concurrently() -> None:
    started: list[str] = []

    async def start(name: str) -> None:
        await asyncio.sleep(0)
        started.append(name)

    runtime = PlatformRuntime(
        (
            RuntimeComponent("a", start=lambda: start("a")),
            RuntimeComponent("b", start=lambda: start("b")),
        )
    )

    await runtime.start()
    snapshot = runtime.snapshot()

    assert snapshot["status"] == "ready"
    assert snapshot["ready_count"] == 2
    assert {item["name"] for item in snapshot["components"]} == {"a", "b"}
    assert started == ["a", "b"] or started == ["b", "a"]


@pytest.mark.asyncio
async def test_runtime_isolates_component_start_failure() -> None:
    async def broken() -> None:
        raise RuntimeError("boom")

    runtime = PlatformRuntime(
        (
            RuntimeComponent("broken", start=broken),
            RuntimeComponent("healthy"),
        )
    )

    await runtime.start()
    snapshot = runtime.snapshot()

    assert snapshot["status"] == "degraded"
    assert snapshot["ready_count"] == 1
    assert snapshot["degraded_count"] == 1
    states = {item["name"]: item["state"] for item in snapshot["components"]}
    assert states == {"broken": ComponentState.DEGRADED.value, "healthy": ComponentState.READY.value}
