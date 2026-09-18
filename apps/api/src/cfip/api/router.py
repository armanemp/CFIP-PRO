"""API router composition."""

from fastapi import APIRouter

from cfip.api.routes.analysis import router as analysis_router
from cfip.api.routes.health import router as health_router
from cfip.api.routes.market import router as market_router

api_router = APIRouter()
api_router.include_router(health_router, tags=["health"])
api_router.include_router(analysis_router, tags=["analysis"])
api_router.include_router(market_router, tags=["market"])
