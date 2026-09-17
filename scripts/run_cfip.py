"""Build the static web application and serve the complete CFIP entrypoint on port 8000."""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "apps" / "web"
WEB_INDEX = WEB / "out" / "index.html"


def main() -> int:
    if not WEB_INDEX.is_file():
        print("CFIP-PRO web build not found; building apps/web...", flush=True)
        subprocess.run(["npm", "run", "build"], cwd=WEB, check=True)

    env = os.environ.copy()
    env.setdefault("PYTHONPATH", str(ROOT / "apps" / "api" / "src"))
    env.setdefault("API_HOST", "127.0.0.1")
    env.setdefault("API_PORT", "8000")

    command = [
        sys.executable,
        "-m",
        "uvicorn",
        "cfip.main:app",
        "--app-dir",
        str(ROOT / "apps" / "api" / "src"),
        "--host",
        env["API_HOST"],
        "--port",
        env["API_PORT"],
    ]
    print("CFIP-PRO is available at http://127.0.0.1:8000", flush=True)
    return subprocess.call(command, cwd=ROOT, env=env)


if __name__ == "__main__":
    raise SystemExit(main())
