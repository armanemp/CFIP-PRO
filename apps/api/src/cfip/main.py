"""FastAPI application entry point and same-origin web serving boundary."""

from pathlib import Path
from uuid import uuid4


from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.middleware.trustedhost import TrustedHostMiddleware
from starlette.requests import Request
from starlette.responses import Response
from fastapi.staticfiles import StaticFiles

from cfip.api.router import api_router
from cfip.core.config import get_settings

settings = get_settings()

class RequestIdMiddleware(BaseHTTPMiddleware):
    """Bound and propagate a correlation ID without trusting arbitrary input."""

    async def dispatch(self, request: Request, call_next) -> Response:
        incoming = request.headers.get("X-Request-ID", "").strip()
        request_id = incoming if 1 <= len(incoming) <= 128 and incoming.isprintable() else str(uuid4())
        response = await call_next(request)
        response.headers["X-Request-ID"] = request_id
        return response


class SecurityHeadersMiddleware(BaseHTTPMiddleware):
    """Apply conservative browser security headers at the API/web boundary."""

    async def dispatch(self, request: Request, call_next) -> Response:
        response = await call_next(request)
        response.headers.setdefault("X-Content-Type-Options", "nosniff")
        response.headers.setdefault("X-Frame-Options", "DENY")
        response.headers.setdefault("Referrer-Policy", "strict-origin-when-cross-origin")
        response.headers.setdefault("Permissions-Policy", "camera=(), microphone=(), geolocation=()")
        response.headers.setdefault("Content-Security-Policy", "frame-ancestors 'none'")
        if settings.app_env.lower() in {"production", "prod"}:
            response.headers.setdefault("Strict-Transport-Security", "max-age=31536000; includeSubDomains")
        return response

app = FastAPI(title=settings.app_name, version=settings.app_version)
app.add_middleware(TrustedHostMiddleware, allowed_hosts=settings.trusted_host_list)
app.add_middleware(RequestIdMiddleware)
app.add_middleware(SecurityHeadersMiddleware)
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=True,
    allow_methods=["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    allow_headers=["Accept", "Authorization", "Content-Type", "If-None-Match", "X-Request-ID"],
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
