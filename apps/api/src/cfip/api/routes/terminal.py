"""Terminal control-plane manifest and contract validation endpoints."""
from fastapi import APIRouter

from cfip.domain.alert_contracts import AlertRule
from cfip.domain.platform_settings import PlatformSettings
from cfip.domain.workspace_contracts import ChartWorkspace

router = APIRouter(prefix="/terminal", tags=["terminal"])

_TERMINAL_FEATURES = (
    "chart","chart-types","crosshair","drawings","object-manager","indicators","multi-pane",
    "multi-timeframe","compare","watchlist","screener","market-depth","time-sales","replay",
    "alerts","notifications","risk","paper-trading","orders","positions","portfolio","journal",
    "research","elyrava","templates","workspaces","command-palette","export",
)

@router.get("/manifest")
async def terminal_manifest() -> dict[str, object]:
    settings = PlatformSettings()
    return {
        "schema_version": 1,
        "features": list(_TERMINAL_FEATURES),
        "defaults": settings.chart.model_dump(),
        "enabled_modules": list(settings.enabled_modules),
        "governance": settings.git.model_dump(),
    }

@router.post("/workspace/validate", response_model=ChartWorkspace)
async def validate_workspace(workspace: ChartWorkspace) -> ChartWorkspace:
    return workspace

@router.post("/alerts/validate", response_model=AlertRule)
async def validate_alert(rule: AlertRule) -> AlertRule:
    return rule
