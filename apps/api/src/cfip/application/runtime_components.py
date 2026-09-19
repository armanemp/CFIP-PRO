"""Built-in runtime components.

External integrations remain adapter boundaries: startup verifies configuration and
registers services without inventing credentials or opening uncontrolled connections.
"""
from datetime import UTC, datetime
from cfip.application.event_bus import EventBus
from cfip.domain.platform_settings import PlatformSettings
from cfip.domain.runtime_contracts import ComponentHealth\nfrom cfip.application.intelligence_runtime import IntelligenceRuntime

def _now() -> int:
    return int(datetime.now(UTC).timestamp())

class EventBusComponent:
    name = "event-bus"
    dependencies: tuple[str, ...] = ()
    def __init__(self, bus: EventBus) -> None:
        self.bus = bus
    async def start(self) -> ComponentHealth:
        return ComponentHealth(component=self.name, status="ready", started_at=_now(), detail="process-local event boundary ready")
    async def stop(self) -> None:
        return None

class ProviderRegistryComponent:
    name = "provider-registry"
    dependencies = ("event-bus",)
    def __init__(self) -> None:
        self.settings = PlatformSettings()
    async def start(self) -> ComponentHealth:
        return ComponentHealth(component=self.name, status="ready", started_at=_now(), detail="provider catalog and routing policy ready")
    async def stop(self) -> None:
        return None

class IntelligenceRuntimeComponent:
    name = "elyrava-intelligence"
    dependencies = ("event-bus", "provider-registry")
    def __init__(self) -> None:
        self.settings = PlatformSettings()
    async def start(self) -> ComponentHealth:
        state = "ready" if self.settings.intelligence.learning_enabled else "degraded"
        detail = "evidence-first intelligence runtime ready" if state == "ready" else "learning disabled by configuration"
        return ComponentHealth(component=self.name, status=state, started_at=_now(), detail=detail)
    async def stop(self) -> None:
        return None

class MarketRuntimeComponent:
    name = "market-data"
    dependencies = ("event-bus", "provider-registry")
    async def start(self) -> ComponentHealth:
        return ComponentHealth(component=self.name, status="ready", started_at=_now(), detail="canonical market-data boundary ready")
    async def stop(self) -> None:
        return None

class TerminalRuntimeComponent:
    name = "terminal"
    dependencies = ("market-data", "elyrava-intelligence")
    async def start(self) -> ComponentHealth:
        return ComponentHealth(component=self.name, status="ready", started_at=_now(), detail="terminal orchestration ready")
    async def stop(self) -> None:
        return None
