"""Provider-neutral indicator plugin contract for OSS-backed and native adapters."""

from collections.abc import Mapping, Sequence
from typing import Protocol
from pydantic import BaseModel, ConfigDict, Field


class IndicatorRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    indicator_id: str = Field(min_length=1, max_length=128)
    symbol: str = Field(min_length=1, max_length=64)
    timeframe: str = Field(min_length=1, max_length=16)
    parameters: Mapping[str, float | int | str | bool] = Field(default_factory=dict)
    values: Sequence[float] = Field(min_length=1, max_length=100_000)


class IndicatorPoint(BaseModel):
    model_config = ConfigDict(extra="forbid")
    timestamp: int = Field(gt=0)
    value: float


class IndicatorResult(BaseModel):
    model_config = ConfigDict(extra="forbid")
    indicator_id: str
    version: str
    points: list[IndicatorPoint]
    metadata: Mapping[str, str] = Field(default_factory=dict)


class IndicatorPlugin(Protocol):
    id: str
    version: str

    def calculate(self, request: IndicatorRequest) -> IndicatorResult: ...
