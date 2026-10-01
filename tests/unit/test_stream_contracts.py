"""Tests for transport-neutral realtime ordering and deduplication."""

from cfip.domain.stream_contracts import StreamCursor, StreamEvent, StreamKey, classify_event


def event(event_id: str, occurred_at: int, sequence: int | None = None) -> StreamEvent:
    return StreamEvent(
        key=StreamKey(provider="test", instrument="EUR/USD", kind="tick"),
        event_id=event_id,
        occurred_at=occurred_at,
        sequence=sequence,
    )


def test_first_event_is_accepted() -> None:
    assert classify_event(event("a", 100, 1), None) == "accept"


def test_duplicate_event_is_rejected_deterministically() -> None:
    cursor = StreamCursor(event_id="a", occurred_at=100, sequence=1)
    assert classify_event(event("a", 100, 1), cursor) == "duplicate"


def test_sequence_controls_order_when_available() -> None:
    cursor = StreamCursor(event_id="a", occurred_at=200, sequence=10)
    assert classify_event(event("b", 100, 9), cursor) == "out_of_order"
    assert classify_event(event("c", 300, 11), cursor) == "accept"


def test_timestamp_controls_order_without_sequence() -> None:
    cursor = StreamCursor(event_id="a", occurred_at=200)
    assert classify_event(event("b", 199), cursor) == "out_of_order"
    assert classify_event(event("c", 201), cursor) == "accept"
