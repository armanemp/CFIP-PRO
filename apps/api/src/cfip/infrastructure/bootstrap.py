"""Local startup/shutdown bootstrap for CFIP platform components.

This layer initializes in-process registries and lifecycle state only. It never
claims external provider connectivity; adapters must report that separately.
"""

from __future__ import annotations

from datetime import UTC, datetime


class PlatformBootstrap:
    def __init__(self) -> None:
        self._initialized: dict[str, datetime] = {}

    async def start(self, name: str) -> None:
        self._initialized[name] = datetime.now(UTC)

    async def stop(self, name: str) -> None:
        self._initialized.pop(name, None)

    def initialized(self, name: str) -> bool:
        return name in self._initialized

    def snapshot(self) -> dict[str, str]:
        return {name: value.isoformat() for name, value in self._initialized.items()}


platform_bootstrap = PlatformBootstrap()
