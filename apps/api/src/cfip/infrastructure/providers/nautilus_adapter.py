"""Optional NautilusTrader boundary for deterministic backtesting."""
from __future__ import annotations
import importlib.metadata
from typing import Any
from pydantic import BaseModel, ConfigDict, Field

class NautilusUnavailable(RuntimeError):
    """Raised when NautilusTrader is unavailable."""

class BacktestRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    strategy_name: str = Field(min_length=1, max_length=128)
    data: list[dict[str, Any]] = Field(min_length=1, max_length=1_000_000)
    parameters: dict[str, float | int | str | bool] = Field(default_factory=dict)

class BacktestResult(BaseModel):
    model_config = ConfigDict(extra="forbid")
    engine: str
    engine_version: str
    strategy_name: str
    status: str
    provenance: str
    metrics: dict[str, float] = Field(default_factory=dict)

class NautilusBacktestAdapter:
    id = "nautilus-trader"

    def available(self) -> bool:
        try:
            import nautilus_trader  # noqa: F401
        except ImportError:
            return False
        return True

    @property
    def version(self) -> str:
        try:
            return importlib.metadata.version("nautilus_trader")
        except importlib.metadata.PackageNotFoundError as exc:
            raise NautilusUnavailable("NautilusTrader is not installed") from exc

    def run(self, request: BacktestRequest) -> BacktestResult:
        if not self.available():
            raise NautilusUnavailable("NautilusTrader is not installed")
        raise NotImplementedError("nautilus_strategy_binding_required")
