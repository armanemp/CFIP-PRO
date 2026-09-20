"""FastAPI application entry point and same-origin web serving boundary."""

from contextlib import asynccontextmanager
from pathlib import Path
from collections.abc import AsyncIterator

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles

from cfip.api.router import api_router
from cfip.core.config import get_settings
from cfip.infrastructure.runtime import platform_runtime

settings = get_settings()


@asynccontextmanager
async def lifespan(_: FastAPI) -> AsyncIterator[None]:
    await platform_runtime.start()
    try:
        yield
    finally:
        await platform_runtime.stop()


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
