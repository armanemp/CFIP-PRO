"""FastAPI application entry point and same-origin web serving boundary."""

from contextlib import asynccontextmanager
from pathlib import Path
from collections.abc import AsyncIterator

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.middleware.trustedhost import TrustedHostMiddleware
from uuid import uuid4
from fastapi.staticfiles import StaticFiles

from cfip.api.router import api_router
from cfip.core.config import get_settings
from cfip.infrastructure.runtime import platform_runtime

settings = get_settings()

class SecurityHeadersMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request, call_next):
        request_id = request.headers.get("X-Request-ID") or uuid4().hex
        response = await call_next(request)
        if settings.security_headers_enabled:
            response.headers["X-Request-ID"] = request_id
            response.headers["X-Content-Type-Options"] = "nosniff"
            response.headers["X-Frame-Options"] = "DENY"
            response.headers["Referrer-Policy"] = "strict-origin-when-cross-origin"
            response.headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()"
            response.headers["Content-Security-Policy"] = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; form-action 'self'; upgrade-insecure-requests"
            response.headers["X-Permitted-Cross-Domain-Policies"] = "none"
            response.headers["Cross-Origin-Opener-Policy"] = "same-origin"
            response.headers["Cross-Origin-Resource-Policy"] = "same-origin"
            if request.url.path.startswith("/api/admin") or request.url.path.startswith("/api/config"):
                response.headers["Cache-Control"] = "no-store"
            if settings.hsts_enabled:
                response.headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains"
        return response




@asynccontextmanager
async def lifespan(_: FastAPI) -> AsyncIterator[None]:
    await platform_runtime.start()
    try:
        yield
    finally:
        await platform_runtime.stop()


app = FastAPI(title=settings.app_name, version=settings.app_version, lifespan=lifespan)
app.add_middleware(TrustedHostMiddleware, allowed_hosts=settings.trusted_host_list)
app.add_middleware(SecurityHeadersMiddleware)
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=True,
    allow_methods=["GET", "HEAD", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    allow_headers=["Accept", "Content-Type", "Authorization", "X-Admin-Control-Token", "X-Request-ID"],
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
