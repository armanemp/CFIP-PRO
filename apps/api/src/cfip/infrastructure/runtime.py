"""Platform lifecycle orchestration.

Registered local components are started concurrently at application startup. This is
an orchestration/readiness boundary; external connectivity is only reported when a
real adapter explicitly marks its component healthy.
"""

from __future__ import annotations

import asyncio
from datetime import UTC, datetime
from typing import Final

from cfip.domain.runtime_contracts import ComponentState, ComponentStatus, RuntimeComponent

DEFAULT_COMPONENTS: Final[tuple[RuntimeComponent, ...]] = tuple(
    RuntimeComponent(name=name)
    for name in (
        "api",
        "market-data",
        "analysis",
        "indicators",
        "intelligence",
        "research",
        "risk",
        "notifications",
        "replay",
        "self-healing",
        "self-development",
        "git-governance",
    )
)


class PlatformRuntime:
    """Shared in-process lifecycle registry for API and platform orchestration."""

    def __init__(self, components: tuple[RuntimeComponent, ...] = DEFAULT_COMPONENTS) -> None:
        self._components = {item.name: item for item in components}
        self._status = {
            item.name: ComponentStatus(name=item.name, required=item.required)
            for item in components
        }
        self._started_at: datetime | None = None
        self._generation = 0

    async def start(self) -> None:
        self._generation += 1
        self._started_at = datetime.now(UTC)
        for status in self._status.values():
            status.state = ComponentState.STARTING
            status.started_at = self._started_at
            status.ready_at = None
            status.error = None
            status.detail = "startup orchestration in progress"
            status.checks = ["contract-loaded"]

        async def start_one(component: RuntimeComponent) -> None:
            status = self._status[component.name]
            try:
                await component.start_component()
            except Exception as exc:
                status.state = ComponentState.DEGRADED
                status.detail = "component startup failed"
                status.error = type(exc).__name__
                status.checks.append("startup-failed")
                return
            status.state = ComponentState.READY
            status.ready_at = datetime.now(UTC)
            status.detail = "local lifecycle boundary initialized"
            status.checks.append("startup-complete")

        await asyncio.gather(*(start_one(item) for item in self._components.values()))

    async def stop(self) -> None:
        async def stop_one(component: RuntimeComponent) -> None:
            status = self._status[component.name]
            try:
                await component.stop_component()
            finally:
                status.state = ComponentState.STOPPED
                status.detail = "shutdown complete"

        await asyncio.gather(*(stop_one(item) for item in self._components.values()))

    def snapshot(self) -> dict[str, object]:
        statuses = tuple(self._status.values())
        required = tuple(item for item in statuses if item.required)
        overall = (
            ComponentState.READY.value
            if required and all(item.state == ComponentState.READY for item in required)
            else ComponentState.DEGRADED.value
        )
        return {
            "status": overall,
            "generation": self._generation,
            "started_at": self._started_at.isoformat() if self._started_at else None,
            "component_count": len(statuses),
            "ready_count": sum(item.state == ComponentState.READY for item in statuses),
            "degraded_count": sum(item.state == ComponentState.DEGRADED for item in statuses),
            "components": [item.as_dict() for item in statuses],
        }

    def mark(
        self,
        name: str,
        state: ComponentState,
        detail: str = "",
        checks: list[str] | None = None,
        error: str | None = None,
    ) -> None:
        status = self._status[name]
        status.state = state
        status.detail = detail
        status.error = error
        if state == ComponentState.READY and status.ready_at is None:
            status.ready_at = datetime.now(UTC)
        if checks is not None:
            status.checks = list(checks)


platform_runtime = PlatformRuntime()
