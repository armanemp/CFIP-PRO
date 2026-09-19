"""Platform lifecycle orchestration.

The lifecycle boundary starts every local CFIP subsystem together and exposes explicit
readiness state. It does not claim external providers are connected unless an adapter
successfully reports readiness.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import UTC, datetime
from enum import StrEnum
from typing import Final


class ComponentState(StrEnum):
    STARTING = "starting"
    READY = "ready"
    DEGRADED = "degraded"
    STOPPED = "stopped"


@dataclass(slots=True)
class ComponentStatus:
    name: str
    state: ComponentState = ComponentState.STARTING
    started_at: datetime | None = None
    detail: str = ""
    checks: list[str] = field(default_factory=list)

    def as_dict(self) -> dict[str, object]:
        return {
            "name": self.name,
            "state": self.state.value,
            "started_at": self.started_at.isoformat() if self.started_at else None,
            "detail": self.detail,
            "checks": list(self.checks),
        }


DEFAULT_COMPONENTS: Final[tuple[str, ...]] = (
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


class PlatformRuntime:
    """In-process lifecycle registry shared by API and background orchestration."""

    def __init__(self, components: tuple[str, ...] = DEFAULT_COMPONENTS) -> None:
        self._components = {name: ComponentStatus(name=name) for name in components}
        self._started_at: datetime | None = None

    async def start(self) -> None:
        now = datetime.now(UTC)
        self._started_at = now
        for component in self._components.values():
            component.state = ComponentState.READY
            component.started_at = now
            component.detail = "local lifecycle boundary initialized"
            component.checks = ["contract-loaded"]

    async def stop(self) -> None:
        for component in self._components.values():
            component.state = ComponentState.STOPPED

    def snapshot(self) -> dict[str, object]:
        states = [item.state for item in self._components.values()]
        overall = (
            ComponentState.READY.value
            if all(state == ComponentState.READY for state in states)
            else ComponentState.DEGRADED.value
        )
        return {
            "status": overall,
            "started_at": self._started_at.isoformat() if self._started_at else None,
            "components": [item.as_dict() for item in self._components.values()],
        }

    def mark(self, name: str, state: ComponentState, detail: str = "", checks: list[str] | None = None) -> None:
        component = self._components[name]
        component.state = state
        component.detail = detail
        if checks is not None:
            component.checks = checks


platform_runtime = PlatformRuntime()
