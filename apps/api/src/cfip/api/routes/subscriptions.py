"""Subscription catalog endpoint; payment mutation remains separately governed."""
from fastapi import APIRouter
from cfip.domain.subscription_contracts import PLANS

router = APIRouter(prefix="/subscriptions", tags=["subscriptions"])


@router.get("/plans")
async def plans() -> list[dict[str, object]]:
    return [plan.model_dump() for plan in PLANS]
