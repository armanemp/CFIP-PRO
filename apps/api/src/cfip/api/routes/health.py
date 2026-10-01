"""Health and readiness endpoints."""

from datetime import UTC, datetime

from fastapi import APIRouter
from pydantic import BaseModel

from cfip.infrastructure.runtime import platform_runtime

router = APIRouter()


class HealthResponse(BaseModel):
    status: str
    service: str
    timestamp: datetime


@router.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    snapshot = platform_runtime.snapshot()
    return HealthResponse(
        status="ok" if snapshot["status"] == "ready" else "degraded",
        service="api",
        timestamp=datetime.now(UTC),
    )


@router.get("/ready")
async def ready() -> dict[str, object]:
    return platform_runtime.snapshot()
