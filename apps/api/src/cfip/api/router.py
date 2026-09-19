"""API router composition."""
from fastapi import APIRouter
from cfip.api.routes.admin import router as admin_router
from cfip.api.routes.analysis import router as analysis_router
from cfip.api.routes.health import router as health_router
from cfip.api.routes.git import router as git_router
from cfip.api.routes.improvement import router as improvement_router
from cfip.api.routes.intelligence import router as intelligence_router
from cfip.api.routes.market import router as market_router
from cfip.api.routes.providers import router as providers_router
from cfip.api.routes.config import router as config_router
from cfip.api.routes.control import router as control_router
from cfip.api.routes.capabilities import router as capabilities_router
from cfip.api.routes.outcomes import router as outcomes_router
from cfip.api.routes.capability_manifest import router as capability_manifest_router
from cfip.api.routes.indicators import router as indicators_router
from cfip.api.routes.risk import router as risk_router
from cfip.api.routes.subscriptions import router as subscriptions_router
from cfip.api.routes.seed import router as seed_router
from cfip.api.routes.terminal import router as terminal_router
from cfip.api.routes.runtime import router as runtime_router
from cfip.api.routes.realtime import router as realtime_router
from cfip.api.routes.orders import router as orders_router

api_router = APIRouter()
api_router.include_router(health_router, tags=["health"])
api_router.include_router(git_router, tags=["git"])
api_router.include_router(control_router, tags=["control"])
api_router.include_router(capabilities_router, tags=["capabilities"])
api_router.include_router(admin_router, tags=["admin"])
api_router.include_router(analysis_router, tags=["analysis"])
api_router.include_router(intelligence_router, tags=["intelligence"])
api_router.include_router(improvement_router, tags=["intelligence"])
api_router.include_router(market_router, tags=["market"])
api_router.include_router(providers_router, tags=["providers"])
api_router.include_router(config_router, tags=["config"])
api_router.include_router(outcomes_router, tags=["outcomes"])
api_router.include_router(capability_manifest_router)
api_router.include_router(indicators_router)
api_router.include_router(risk_router)
api_router.include_router(subscriptions_router)
api_router.include_router(seed_router)
api_router.include_router(terminal_router)
api_router.include_router(runtime_router)
api_router.include_router(realtime_router)
api_router.include_router(orders_router)
