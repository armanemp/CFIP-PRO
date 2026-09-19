"""Terminal control manifest.

This endpoint is read-only and provides frontend-safe contracts/defaults. Mutable settings
remain behind authenticated control-plane persistence.
"""
from fastapi import APIRouter

from cfip.domain.config_contracts import ChartConfig, NotificationConfig, TerminalConfig
from cfip.domain.datafeed_contracts import DatafeedRequest
from cfip.domain.platform_capabilities import capabilities
from cfip.domain.provider_contracts import PROVIDER_CATALOG
from cfip.domain.workspace_contracts import WorkspacePreferences

router = APIRouter(prefix="/terminal", tags=["terminal"])

@router.get("/manifest")
async def manifest() -> dict[str, object]:
    terminal = TerminalConfig()
    chart = ChartConfig()
    return {
        "schema_version": 1,
        "capabilities": [item.model_dump() for item in capabilities()],
        "providers": [item.model_dump() for item in PROVIDER_CATALOG],
        "chart": chart.model_dump(),
        "notifications": NotificationConfig().model_dump(),
        "workspace_preferences": WorkspacePreferences().model_dump(),
        "datafeed_defaults": DatafeedRequest(
            symbol=terminal.default_symbol, venue=terminal.default_venue, timeframe=chart.default_timeframe
        ).model_dump(),
        "terminal": terminal.model_dump(),
        "mutation_enabled": False,
        "secrets_exposed": False,
    }
