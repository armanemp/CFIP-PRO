"""FastAPI application entry point and same-origin web serving boundary."""
from contextlib import asynccontextmanager
from pathlib import Path
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles
from cfip.api.router import api_router
from cfip.application.event_bus import EventBus
from cfip.application.paper_oms import PaperOMS
from cfip.application.runtime_components import EventBusComponent, IntelligenceRuntimeComponent, MarketRuntimeComponent, ProviderRegistryComponent, TerminalRuntimeComponent
from cfip.application.runtime_supervisor import RuntimeSupervisor
from cfip.core.config import get_settings

settings = get_settings()
event_bus = EventBus()
paper_oms = PaperOMS()
runtime_supervisor = RuntimeSupervisor((
    EventBusComponent(event_bus),
    ProviderRegistryComponent(),
    IntelligenceRuntimeComponent(),
    MarketRuntimeComponent(),
    TerminalRuntimeComponent(),
))

@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.event_bus = event_bus
    app.state.paper_oms = paper_oms
    app.state.runtime_supervisor = runtime_supervisor
    await runtime_supervisor.start()
    try:
        yield
    finally:
        await runtime_supervisor.stop()

app = FastAPI(title=settings.app_name, version=settings.app_version, lifespan=lifespan)
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
app.include_router(api_router, prefix="/api")

PROJECT_ROOT = Path(__file__).resolve().parents[4]
WEB_ROOT = PROJECT_ROOT / "apps" / "web" / "out"
WEB_INDEX = WEB_ROOT / "index.html"

@app.get("/", tags=["meta"], response_model=None)
async def root() -> FileResponse | dict[str, str]:
    if WEB_INDEX.is_file():
        return FileResponse(WEB_INDEX, media_type="text/html")
    return {"name": settings.app_name, "version": settings.app_version, "status": "ok"}

if WEB_ROOT.is_dir():
    app.mount("/", StaticFiles(directory=WEB_ROOT, html=True), name="web")
