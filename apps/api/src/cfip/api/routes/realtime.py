"""Realtime terminal event transport.

The browser receives normalized events from the same event boundary used by backend
services. A future NATS JetStream adapter can replace the process-local bus.
"""
from fastapi import APIRouter, Request, WebSocket, WebSocketDisconnect
from cfip.application.event_bus import EventBus

router = APIRouter(tags=["realtime"])

def _bus(request: Request) -> EventBus:
    value = getattr(request.app.state, "event_bus", None)
    if value is None:
        raise RuntimeError("event bus is not initialized")
    return value

@router.websocket("/ws/terminal/{topic}")
async def terminal_events(websocket: WebSocket, topic: str) -> None:
    await websocket.accept()
    bus = _bus(websocket.app)
    try:
        async for event in bus.subscribe(topic):
            await websocket.send_json(event)
    except WebSocketDisconnect:
        return
