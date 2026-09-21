"""Compatibility launcher for the historical CFIP entrypoint."""
from scripts.run_mios import main

if __name__ == "__main__":
    raise SystemExit(main())
