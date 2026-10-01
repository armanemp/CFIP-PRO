"""Compatibility launcher for the historical MIOS entrypoint."""
from scripts.run_miosai import main

if __name__ == "__main__":
    raise SystemExit(main())
