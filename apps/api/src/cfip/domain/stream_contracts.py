"""Provider-neutral realtime stream contracts.

The stream boundary is intentionally transport-agnostic: WebSocket, NATS JetStream,
polling and replay adapters normalize into the same event/cursor semantics.
"""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

StreamKind = Literal["tick", "bar", "orderbook", "news", "analysis", "alert"]
StreamDecision = Literal["accept", "duplicate", "out_of_order"]


class StreamKey(BaseModel):
    model_config = ConfigDict(frozen=True, extra="forbid")

    provider: str = Field(min_length=1, max_length=80)
    instrument: str = Field(min_length=1, max_length=64)
    kind: StreamKind
    timeframe: str | None = Field(default=None, max_length=16)


class StreamEvent(BaseModel):
    model_config = ConfigDict(extra="forbid")

    key: StreamKey
    occurred_at: int = Field(gt=0)
    sequence: int | None = Field(default=None, ge=0)
    event_id: str = Field(min_length=1, max_length=160)
    payload: dict[str, object] = Field(default_factory=dict)


class StreamCursor(BaseModel):
    model_config = ConfigDict(frozen=True, extra="forbid")

    sequence: int | None = Field(default=None, ge=0)
    occurred_at: int = Field(gt=0)
    event_id: str = Field(min_length=1, max_length=160)


def event_order(event: StreamEvent) -> tuple[int, int, str]:
    """Return a deterministic order key; sequence wins when a provider supplies one."""
    return (
        event.sequence if event.sequence is not None else -1,
        event.occurred_at,
        event.event_id,
    )


def classify_event(event: StreamEvent, cursor: StreamCursor | None) -> StreamDecision:
    if cursor is None:
        return "accept"
    if event.event_id == cursor.event_id:
        return "duplicate"
    if event.sequence is not None and cursor.sequence is not None:
        if event.sequence < cursor.sequence:
            return "out_of_order"
        if event.sequence == cursor.sequence:
            return "duplicate"
        return "accept"
    if event.occurred_at < cursor.occurred_at:
        return "out_of_order"
    return "accept"
