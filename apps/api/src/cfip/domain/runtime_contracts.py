"""Provider-neutral lifecycle contracts for CFIP runtime orchestration."""

from __future__ import annotations

from collections.abc import Awaitable, Callable
from dataclasses import dataclass, field
from datetime import datetime
from enum import StrEnum
from typing import Final

RUNTIME_CHECK_CONTRACT: Final[str] = "cfip.runtime.check.v1"
RuntimeStart = Callable[[], Awaitable[None]]
RuntimeStop = Callable[[], Awaitable[None]]


class ComponentState(StrEnum):
    STARTING = "starting"
    READY = "ready"
    DEGRADED = "degraded"
    STOPPED = "stopped"


@dataclass(slots=True)
class ComponentStatus:
    name: str
    required: bool = True
    state: ComponentState = ComponentState.STARTING
    started_at: datetime | None = None
    ready_at: datetime | None = None
    detail: str = ""
    checks: list[str] = field(default_factory=list)
    error: str | None = None

    def as_dict(self) -> dict[str, object]:
        duration_ms = None
        if self.started_at and self.ready_at:
            duration_ms = round((self.ready_at - self.started_at).total_seconds() * 1000, 2)
        return {
            "name": self.name,
            "required": self.required,
            "state": self.state.value,
            "started_at": self.started_at.isoformat() if self.started_at else None,
            "ready_at": self.ready_at.isoformat() if self.ready_at else None,
            "duration_ms": duration_ms,
            "detail": self.detail,
            "checks": list(self.checks),
            "error": self.error,
        }


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
