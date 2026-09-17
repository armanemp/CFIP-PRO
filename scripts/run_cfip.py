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
    command = shutil.which("npm.cmd") or shutil.which("npm")
    if command is None:
        raise RuntimeError("npm was not found in PATH. Install Node.js/npm and restart PowerShell.")
    return command


def _web_build_is_stale() -> bool:
    if not WEB_INDEX.is_file():
        return True
    output_mtime = WEB_INDEX.stat().st_mtime_ns
    tracked_roots = (WEB / "src", WEB / "public", WEB / "package.json", WEB / "next.config.ts", WEB / "next.config.mjs")
    for root in tracked_roots:
        if not root.exists():
            continue
        if root.is_file() and root.stat().st_mtime_ns > output_mtime:
            return True
        if root.is_dir():
            for path in root.rglob("*"):
                if path.is_file() and path.stat().st_mtime_ns > output_mtime:
                    return True
    return False


def _build_web() -> None:
    npm = _npm_command()
    print("CFIP-PRO web build is missing or stale; building apps/web...", flush=True)
    subprocess.run([npm, "run", "build"], cwd=WEB, check=True)
    if not WEB_INDEX.is_file():
        raise RuntimeError("Next.js build completed without creating apps/web/out/index.html")


def _install_windows_disconnect_filter(loop: asyncio.AbstractEventLoop) -> None:
    previous_handler = loop.get_exception_handler()

    def handle_exception(current_loop, context):
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
    config = uvicorn.Config("cfip.main:app", host=env["API_HOST"], port=int(env["API_PORT"]))
    server = uvicorn.Server(config)
    try:
        await server.serve()
    except (asyncio.CancelledError, KeyboardInterrupt):
        server.should_exit = True


def main() -> int:
    if _web_build_is_stale():
        _build_web()
    print("CFIP-PRO is available at http://127.0.0.1:8000", flush=True)
    asyncio.run(_serve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
