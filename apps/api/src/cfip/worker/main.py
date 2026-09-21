"""Worker process entry point.

The worker is intentionally thin. Capability-owned consumers will be added behind
explicit application/infrastructure boundaries as vertical slices are implemented.
"""

import asyncio


async def run() -> None:
    while True:
        await asyncio.sleep(3600)


if __name__ == "__main__":
    asyncio.run(run())
