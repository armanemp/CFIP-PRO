"""Run the native CFIP-PRO application from a single Windows entrypoint."""

from __future__ import annotations

import asyncio
import os
import shutil
import subprocess
import sys
from pathlib import Path

import uvicorn

ROOT = Path(__file__).resolve().parents[1]
API_SRC = ROOT / "apps" / "api" / "src"
WEB = ROOT / "apps" / "web"
WEB_INDEX = WEB / "out" / "index.html"


def _npm_command() -> str:
    """Return the Windows npm command that can be launched by the native runner."""
    command = shutil.which("npm.cmd") or shutil.which("npm")
    if command is None:
        raise RuntimeError(
            "npm was not found in PATH. Install Node.js/npm and restart PowerShell."
        )
    return command


def _web_source_mtime() -> float:
    """Return the newest frontend source timestamp, excluding generated output."""
    newest = 0.0
    for path in WEB.rglob("*"):
        if not path.is_file() or "out" in path.parts or "node_modules" in path.parts:
            continue
        try:
            newest = max(newest, path.stat().st_mtime)
        except OSError:
            continue
    return newest


def _web_build_required() -> bool:
    """Build when output is missing or older than a tracked frontend source file."""
    if not WEB_INDEX.is_file():
        return True
    try:
        return WEB_INDEX.stat().st_mtime < _web_source_mtime()
    except OSError:
        return True


def _build_web() -> None:
    """Build the exported frontend when the generated output is stale or absent."""
    npm = _npm_command()
    print("CFIP-PRO web build is missing or stale; building apps/web...", flush=True)
    subprocess.run([npm, "run", "build"], cwd=WEB, check=True)
    if not WEB_INDEX.is_file():
        raise RuntimeError("Next.js build completed without creating apps/web/out/index.html")


def _install_windows_disconnect_filter(loop: asyncio.AbstractEventLoop) -> None:
    """Ignore the harmless WinSock reset raised when a browser closes a socket early."""
    previous_handler = loop.get_exception_handler()

    def handle_exception(
        current_loop: asyncio.AbstractEventLoop,
        context: dict[str, object],
    ) -> None:
        exception = context.get("exception")
        if isinstance(exception, ConnectionResetError) and getattr(exception, "winerror", None) == 10054:
            return
        if previous_handler is not None:
            previous_handler(current_loop, context)
        else:
            current_loop.default_exception_handler(context)

    loop.set_exception_handler(handle_exception)


async def _serve() -> None:
    loop = asyncio.get_running_loop()
    _install_windows_disconnect_filter(loop)

    env = os.environ.copy()
    env.setdefault("PYTHONPATH", str(API_SRC))
    env.setdefault("API_HOST", "127.0.0.1")
    env.setdefault("API_PORT", "8000")

    api_path = str(API_SRC)
    if api_path not in sys.path:
        sys.path.insert(0, api_path)

    config = uvicorn.Config(
        "cfip.main:app",
        host=env["API_HOST"],
        port=int(env["API_PORT"]),
    )
    server = uvicorn.Server(config)
    await server.serve()


def main() -> int:
    if _web_build_required():
        _build_web()

    print("CFIP-PRO is available at http://127.0.0.1:8000", flush=True)
    asyncio.run(_serve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
