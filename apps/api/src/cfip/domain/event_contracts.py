"""Stable event envelope used between market, analysis, intelligence and terminal domains."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

EventName = Literal[
    "market.tick","market.bar","analysis.updated","intelligence.completed",
    "alert.triggered","notification.created","risk.breached","execution.updated",
    "research.updated","self_improvement.proposed",
]

class EventEnvelope(BaseModel):
    model_config = ConfigDict(extra="forbid")
    event_id: str = Field(min_length=1, max_length=128)
    event_name: EventName
    occurred_at: int = Field(gt=0)
    correlation_id: str = Field(min_length=1, max_length=128)
    producer: str = Field(min_length=1, max_length=100)
    schema_version: int = Field(default=1, ge=1)
    payload: dict[str, object] = {}
