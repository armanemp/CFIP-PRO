"""Run the native CFIP-PRO application from a single Windows entrypoint."""

from __future__ import annotations

import asyncio
import os
import shutil
from pathlib import Path

import uvicorn

ROOT = Path(__file__).resolve().parents[1]
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


def _build_web() -> None:
    """Build the exported frontend only when the expected output is absent."""
    import subprocess

    npm = _npm_command()
    print("CFIP-PRO web build not found; building apps/web...", flush=True)
    subprocess.run([npm, "run", "build"], cwd=WEB, check=True)
    if not WEB_INDEX.is_file():
        raise RuntimeError("Next.js build completed without creating apps/web/out/index.html")


def _install_windows_disconnect_filter(loop: asyncio.AbstractEventLoop) -> None:
    """Ignore the harmless WinSock reset raised when a browser closes a socket early.

    Windows' Proactor transport can report WSAECONNRESET (10054) from its callback
    after the peer has already closed the HTTP connection. It is not an application
    failure and should not pollute the native developer console with a traceback.
    """
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
    env.setdefault("PYTHONPATH", str(ROOT / "apps" / "api" / "src"))
    env.setdefault("API_HOST", "127.0.0.1")
    env.setdefault("API_PORT", "8000")

    config = uvicorn.Config(
        "cfip.main:app",
        app_dir=str(ROOT / "apps" / "api" / "src"),
        host=env["API_HOST"],
        port=int(env["API_PORT"]),
    )
    server = uvicorn.Server(config)
    await server.serve()


def main() -> int:
    if not WEB_INDEX.is_file():
        _build_web()

    print("CFIP-PRO is available at http://127.0.0.1:8000", flush=True)
    asyncio.run(_serve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
