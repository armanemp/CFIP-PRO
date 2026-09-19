"""Contracts for deterministic application startup and component health."""
from typing import Literal, Protocol
from pydantic import BaseModel, ConfigDict, Field

ComponentStatus = Literal["starting", "ready", "degraded", "failed", "stopped"]

class ComponentHealth(BaseModel):
    model_config = ConfigDict(extra="forbid")
    component: str = Field(min_length=1, max_length=120)
    status: ComponentStatus
    started_at: int | None = Field(default=None, gt=0)
    detail: str = ""
    dependencies: tuple[str, ...] = ()

class RuntimeSnapshot(BaseModel):
    model_config = ConfigDict(extra="forbid")
    status: Literal["starting", "ready", "degraded", "failed", "stopped"]
    components: tuple[ComponentHealth, ...] = ()

class RuntimeComponent(Protocol):
    name: str
    dependencies: tuple[str, ...]
    async def start(self) -> ComponentHealth: ...
    async def stop(self) -> None: ...
