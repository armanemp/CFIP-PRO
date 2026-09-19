"""Lifecycle contracts for the CFIP platform runtime.

The runtime owns orchestration state only. Concrete adapters remain responsible for
real external connectivity and must report it through explicit health signals.
"""

from __future__ import annotations

from collections.abc import Awaitable, Callable
from dataclasses import dataclass
from typing import Final

from cfip.infrastructure.runtime import ComponentStatus, ComponentState

RuntimeStart = Callable[[], Awaitable[None]]
RuntimeStop = Callable[[], Awaitable[None]]

RUNTIME_CHECK_CONTRACT: Final[str] = "cfip.runtime.check.v1"


@dataclass(frozen=True, slots=True)
class RuntimeComponent:
    name: str
    start: RuntimeStart | None = None
    stop: RuntimeStop | None = None
    required: bool = True

    async def start_component(self) -> None:
        if self.start is not None:
            await self.start()

    async def stop_component(self) -> None:
        if self.stop is not None:
            await self.stop()


def component_ready(status: ComponentStatus) -> bool:
    return status.state == ComponentState.READY


def component_healthy(status: ComponentStatus) -> bool:
    return status.state in {ComponentState.READY, ComponentState.DEGRADED}
