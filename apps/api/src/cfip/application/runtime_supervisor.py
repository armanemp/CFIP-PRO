"""Startup/shutdown orchestration for the whole CFIP runtime."""
import asyncio
from datetime import UTC, datetime

from cfip.domain.runtime_contracts import ComponentHealth, RuntimeComponent, RuntimeSnapshot

class RuntimeSupervisor:
    def __init__(self, components: tuple[RuntimeComponent, ...]) -> None:
        self._components = components
        self._health: dict[str, ComponentHealth] = {}
        self._started = False

    @staticmethod
    def _now() -> int:
        return int(datetime.now(UTC).timestamp())

    async def start(self) -> RuntimeSnapshot:
        pending = {component.name: component for component in self._components}
        while pending:
            ready_names = set(self._health)
            batch = [
                component for component in pending.values()
                if set(component.dependencies).issubset(ready_names)
            ]
            if not batch:
                names = ", ".join(sorted(pending))
                raise RuntimeError(f"runtime dependency cycle or missing dependency: {names}")
            results = await asyncio.gather(*(self._start_one(c) for c in batch), return_exceptions=True)
            for component, result in zip(batch, results, strict=True):
                if isinstance(result, Exception):
                    self._health[component.name] = ComponentHealth(
                        component=component.name, status="failed", started_at=self._now(), detail=str(result),
                        dependencies=component.dependencies,
                    )
                    raise result
                self._health[component.name] = result
                pending.pop(component.name, None)
        self._started = True
        return self.snapshot()

    async def _start_one(self, component: RuntimeComponent) -> ComponentHealth:
        health = await component.start()
        return health

    async def stop(self) -> None:
        for component in reversed(self._components):
            if component.name in self._health:
                try:
                    await component.stop()
                finally:
                    self._health[component.name] = ComponentHealth(
                        component=component.name, status="stopped", dependencies=component.dependencies
                    )
        self._started = False

    def snapshot(self) -> RuntimeSnapshot:
        values = tuple(self._health[name] for name in sorted(self._health))
        failed = any(item.status == "failed" for item in values)
        degraded = any(item.status == "degraded" for item in values)
        stopped = bool(values) and all(item.status == "stopped" for item in values)\n        status = "failed" if failed else "degraded" if degraded else "ready" if self._started else "stopped" if stopped else "starting"
        return RuntimeSnapshot(status=status, components=values)
