"""Native MIOSAI application launcher.

The historical run_cfip.py entrypoint imports this module for compatibility.
"""
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
        raise RuntimeError("npm was not found in PATH")
    return command

def _web_build_is_stale() -> bool:
    if not WEB_INDEX.is_file():
        return True
    output_mtime = WEB_INDEX.stat().st_mtime_ns
    for root in (WEB / "src", WEB / "public", WEB / "package.json", WEB / "next.config.ts", WEB / "next.config.mjs"):
        if not root.exists():
            continue
        if root.is_file() and root.stat().st_mtime_ns > output_mtime:
            return True
        if root.is_dir() and any(p.is_file() and p.stat().st_mtime_ns > output_mtime for p in root.rglob("*")):
            return True
    return False

def _build_web() -> None:
    subprocess.run([_npm_command(), "run", "build"], cwd=WEB, check=True)
    if not WEB_INDEX.is_file():
        raise RuntimeError("MIOSAI web build did not create apps/web/out/index.html")

async def _serve() -> None:
    env = os.environ.copy()
    env.setdefault("PYTHONPATH", str(API_SRC))
    env.setdefault("API_HOST", "127.0.0.1")
    env.setdefault("API_PORT", "8000")
    if str(API_SRC) not in sys.path:
        sys.path.insert(0, str(API_SRC))
    config = uvicorn.Config("cfip.main:app", host=env["API_HOST"], port=int(env["API_PORT"]))
    await uvicorn.Server(config).serve()

def main() -> int:
    if _web_build_is_stale():
        _build_web()
    print("MIOSAI is available at http://127.0.0.1:8000", flush=True)
    asyncio.run(_serve())
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
